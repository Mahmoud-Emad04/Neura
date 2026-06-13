using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neura.Core.Contracts.Community;
using Neura.Core.Hubs;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Neura.Services.Hubs;

/// <summary>
///     The central SignalR Hub for the Discord-like community feature.
///
///     Hybrid Group Strategy (enforced throughout):
///     ┌─────────────────────────────────────────────────────────────┐
///     │  course-{id}   → presence updates + unread notifications   │
///     │  channel-{id}  → full MessageDto payloads                  │
///     └─────────────────────────────────────────────────────────────┘
///
///     Authentication: JWT bearer validated by ASP.NET Core middleware
///     BEFORE the connection reaches this Hub. Every method can trust
///     Context.User is populated and verified.
///
///     This Hub is intentionally thin:
///     - No DbContext references
///     - No EF Core queries
///     - All persistence delegated to <see cref="IChatService"/>
///     - All presence state delegated to <see cref="IPresenceTracker"/>
/// </summary>
[Authorize]
public sealed class CommunityHub(
    IPresenceTracker presenceTracker,
    IChatService chatService,
    IVoiceChannelService voiceService,
    IServiceScopeFactory scopeFactory,
    ILogger<CommunityHub> logger,
    HashidsNet.IHashids hashids)
    : Hub<ICommunityHubClient>
{
    // -------------------------------------------------------------------------
    // Rate Limiting (static — shared across all Hub instances)
    // -------------------------------------------------------------------------

    private static readonly ConcurrentDictionary<string, DateTime> _lastMessageTime = new();
    private static readonly TimeSpan MinMessageInterval = TimeSpan.FromMilliseconds(500);

    // -------------------------------------------------------------------------
    // Connection Lifecycle
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Fires when a client establishes a SignalR connection.
    ///
    ///     Flow:
    ///     1. Extract userId + courseId from JWT claims / query string
    ///     2. Validate course membership (reject unauthorized connections)
    ///     3. Register connection in IPresenceTracker
    ///     4. Join the course-{id} group (lightweight events only)
    ///     5. If first connection → broadcast online presence to course peers
    ///     6. Send back the current online member list to the connecting client
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var courseId = GetCourseId();

        // Security: reject the connection if the user is not a course member
        // (Admins/SuperAdmins bypass via IsCourseMemberByIdAsync)
        var isMember = await chatService.IsCourseMemberByIdAsync(userId, courseId);
        if (!isMember)
        {
            Context.Abort();
            return;
        }

        // Join the course-level group (presence + unread badges only)
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Course(courseId));

        // Register in presence tracker
        var justCameOnline = await presenceTracker.UserConnectedAsync(
            userId, courseId, Context.ConnectionId);

        // If this is their first connection, notify ALL course peers
        if (justCameOnline)
        {
            await Clients
                .GroupExcept(HubGroups.Course(courseId), Context.ConnectionId)
                .PresenceChanged(new PresenceUpdateDto(userId, true));
        }

        // Send the current online member list back to the connecting client ONLY
        // so the UI can hydrate the sidebar without an extra REST call
        var onlineUsers = await presenceTracker.GetOnlineUsersAsync(courseId);
        await Clients.Caller.InitialPresenceSync(onlineUsers);

        await base.OnConnectedAsync();
    }

    /// <summary>
    ///     Fires when a client disconnects (tab closed, network drop, browser crash).
    ///
    ///     Flow:
    ///     1. Remove connection from presence tracker
    ///     2. If they were viewing a channel → leave channel-{id} group
    ///     3. If last connection → broadcast offline status + persist LastSeenAt to SQL
    ///
    ///     Note: Groups.RemoveFromGroupAsync is NOT needed for the course group —
    ///     SignalR removes connections from all groups automatically on disconnect.
    ///     We only need to clean up our presence state.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var result = await presenceTracker.UserDisconnectedAsync(Context.ConnectionId);

        // Unknown connectionId — nothing to clean up (safe early exit)
        if (result is null)
        {
            await base.OnDisconnectedAsync(exception);
            return;
        }

        // If they were actively viewing a channel, leave that group explicitly.
        // While SignalR auto-removes from all groups on disconnect, we call this
        // to keep our logic explicit and future-proof for server-side group auditing.
        if (result.LastChannelId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                HubGroups.Channel(result.LastChannelId.Value));
        }

        // Only broadcast offline + write to SQL when ALL tabs are closed
        if (result.UserWentOffline)
        {
            // Notify course peers
            await Clients
                .Group(HubGroups.Course(result.CourseId))
                .PresenceChanged(new PresenceUpdateDto(result.UserId, IsOnline: false));

            // Fire-and-forget SQL write using an independent DI scope so the
            // DbContext outlives the Hub's own scope (which is disposed after
            // OnDisconnectedAsync returns).
            _ = PersistLastSeenAtSafeAsync(result.UserId);
        }

        // If the user was in a voice channel, remove them and broadcast left.
        _ = LeaveVoiceChannelSafeAsync(result.UserId);

        await base.OnDisconnectedAsync(exception);
    }

    // -------------------------------------------------------------------------
    // Channel Group Management (Hybrid Strategy Core)
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Called when a user CLICKS on a channel in the sidebar.
    ///
    ///     Flow:
    ///     1. Verify the user is a member of the course owning this channel (security)
    ///     2. Remove connection from previous channel-{id} group (if any)
    ///     3. Update active channel in presence tracker
    ///     4. Add connection to new channel-{id} group
    ///
    ///     The client calls this before rendering the message history view.
    ///     After this returns, the client is subscribed to live messages for the channel.
    /// </summary>
    public async Task JoinChannel(int channelId)
    {
        var userId = GetUserId();

        // Security: verify the user belongs to the course that owns this channel.
        // A malicious client could call JoinChannel with any channelId.
        // Admins/SuperAdmins bypass via the role check inside IsCourseMemberAsync.
        var isMember = await chatService.IsCourseMemberAsync(userId, channelId);
        if (!isMember)
        {
            await Clients.Caller.Error("You are not a member of this course.");
            return;
        }

        // Remove from the previously active channel group (if switching channels)
        var previousChannelId = await presenceTracker.UpdateCurrentChannelAsync(
            Context.ConnectionId, channelId);

        if (previousChannelId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                HubGroups.Channel(previousChannelId.Value));
        }

        // Join the new channel group — from this point forward, this connection
        // will receive full MessageDto payloads for this channel
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            HubGroups.Channel(channelId));
    }

    /// <summary>
    ///     Called when a user navigates AWAY from a channel without leaving the course
    ///     (e.g., clicking on the course overview page, or a Voice channel tab).
    ///
    ///     After this call, the connection stops receiving MessageDto payloads
    ///     but remains in the course-{id} group for presence/unread badges.
    /// </summary>
    public async Task LeaveChannel(int channelId)
    {
        // Clear channel from presence tracker
        await presenceTracker.UpdateCurrentChannelAsync(
            Context.ConnectionId, newChannelId: null);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            HubGroups.Channel(channelId));
    }

    // -------------------------------------------------------------------------
    // Messaging
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Called when a user submits a message in the chat input.
    ///
    ///     Flow:
    ///     1. Validate the request payload (SignalR does NOT auto-validate DataAnnotations)
    ///     2. Rate-limit to prevent message spam
    ///     3. Delegate persistence to IChatService (returns hydrated MessageDto)
    ///     4. Broadcast full MessageDto to channel-{id} group ONLY
    ///     5. Broadcast lightweight UnreadNotificationDto to course-{id} group,
    ///        EXCLUDING the sender's own connection
    ///
    ///     Step 5 is the heart of the Hybrid strategy — heavy payload goes to
    ///     channel subscribers only; everyone else gets just a badge ping.
    /// </summary>
    public async Task SendMessage(SendMessageHubRequest request)
    {
        var userId = GetUserId();

        // ── 1. Manual input validation (SignalR skips DataAnnotations) ────
        if (request.ChannelId <= 0)
        {
            await Clients.Caller.Error("Invalid channel ID.");
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 4_000)
        {
            await Clients.Caller.Error("Message content must be between 1 and 4000 characters.");
            return;
        }

        // ── 2. Rate limiting ─────────────────────────────────────────────
        var now = DateTime.UtcNow;
        if (_lastMessageTime.TryGetValue(userId, out var lastTime)
            && (now - lastTime) < MinMessageInterval)
        {
            await Clients.Caller.Error("You are sending messages too quickly.");
            return;
        }
        _lastMessageTime[userId] = now;

        // ── 3. Persist to SQL via service layer ──────────────────────────
        MessageDto messageDto;
        try
        {
            messageDto = await chatService.SaveMessageAsync(
                channelId: request.ChannelId,
                senderId: userId,
                content: request.Content,
                replyToMessageId: request.ReplyToMessageId);
        }
        catch (UnauthorizedAccessException)
        {
            await Clients.Caller.Error("You are not authorized to send messages in this channel.");
            return;
        }

        // ── 4. Broadcast full message to channel-{id} subscribers ────────
        await Clients
            .Group(HubGroups.Channel(request.ChannelId))
            .ReceiveMessage(messageDto);

        // ── 5. Broadcast unread badge to course-{id} group ───────────────
        // SignalR's GroupExcept only accepts connection IDs, not group names.
        // We exclude only the SENDER's own connection here.
        // Users actively in the channel group will ignore the UnreadNotification
        // client-side because their channel is already active/focused.
        var courseId = await chatService.GetCourseIdForChannelAsync(request.ChannelId);

        await Clients
            .GroupExcept(HubGroups.Course(courseId), Context.ConnectionId)
            .UnreadNotification(new UnreadNotificationDto(
                CourseId: courseId,
                ChannelId: request.ChannelId,
                ChannelName: string.Empty
            ));
    }

    // -------------------------------------------------------------------------
    // Voice Channel Events
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Called when a user joins a voice channel.
    ///     Flow: validate membership → join voice room → join voice-{id} group
    ///           → broadcast VoiceParticipantJoined → send participant list to caller.
    /// </summary>
    public async Task JoinVoiceChannel(JoinVoiceRequest request)
    {
        var userId = GetUserId();

        VoiceParticipantDto participant;
        try
        {
            participant = await voiceService.JoinVoiceAsync(
                userId, Context.ConnectionId, request.ChannelId);
        }
        catch (UnauthorizedAccessException)
        {
            await Clients.Caller.Error("You are not a member of this course.");
            return;
        }
        catch (KeyNotFoundException ex)
        {
            await Clients.Caller.Error(ex.Message);
            return;
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.Error(ex.Message);
            return;
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            HubGroups.VoiceChannel(request.ChannelId));

        await Clients
            .Group(HubGroups.VoiceChannel(request.ChannelId))
            .VoiceParticipantJoined(participant);

        var participants = await voiceService.GetParticipantsAsync(
            request.ChannelId, userId);
        await Clients.Caller.InitialVoiceRoomSync(participants);
    }

    /// <summary>
    ///     Called when a user leaves a voice channel.
    ///     Removes connection from voice-{id} group and broadcasts VoiceParticipantLeft.
    /// </summary>
    public async Task LeaveVoiceChannel(int channelId)
    {
        var userId = GetUserId();

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            HubGroups.VoiceChannel(channelId));

        _ = voiceService.LeaveVoiceAsync(userId);

        await Clients
            .Group(HubGroups.VoiceChannel(channelId))
            .VoiceParticipantLeft(userId);
    }

    /// <summary>
    ///     Called when a user mutes / deafens / speaks.
    ///     Updates state and broadcasts the diff to all voice-room peers.
    /// </summary>
    public async Task UpdateVoiceState(UpdateVoiceStateRequest request)
    {
        var userId = GetUserId();

        var currentChannelId = voiceService.GetUserCurrentChannelId(userId);
        if (!currentChannelId.HasValue)
        {
            await Clients.Caller.Error("You are not in a voice channel.");
            return;
        }

        VoiceParticipantDto? updated;
        try
        {
            updated = await voiceService.UpdateStateAsync(
                userId, currentChannelId.Value,
                request.IsMuted, request.IsDeafened, request.IsSpeaking);
        }
        catch (UnauthorizedAccessException)
        {
            await Clients.Caller.Error("You are not a member of this course.");
            return;
        }

        if (updated is null)
        {
            await Clients.Caller.Error("You are not in a voice channel.");
            return;
        }

        await Clients
            .Group(HubGroups.VoiceChannel(currentChannelId.Value))
            .VoiceParticipantStateChanged(
                userId,
                updated.IsMuted,
                updated.IsDeafened,
                updated.IsSpeaking);
    }

    /// <summary>
    ///     CoInstructors+ can kick a user from a voice channel.
    ///     Broadcasts VoiceChannelKicked to the kicked user only,
    ///     and VoiceParticipantLeft to the rest of the room.
    /// </summary>
    public async Task KickFromVoiceChannel(int channelId, string targetUserId)
    {
        var requestingUserId = GetUserId();

        KickResult result;
        try
        {
            result = await voiceService.KickAsync(
                targetUserId, channelId, requestingUserId);
        }
        catch (UnauthorizedAccessException)
        {
            await Clients.Caller.Error(
                "You do not have permission to kick from this voice channel.");
            return;
        }

        // Remove the target connection from the voice group
        var targetConnectionId = voiceService.GetConnectionId(targetUserId);
        if (!string.IsNullOrEmpty(targetConnectionId))
        {
            await Groups.RemoveFromGroupAsync(
                targetConnectionId,
                HubGroups.VoiceChannel(channelId));
        }

        // Notify the kicked user only
        await Clients
            .Group(HubGroups.VoiceChannel(channelId))
            .VoiceChannelKicked(targetUserId);

        // Notify room peers
        await Clients
            .Group(HubGroups.VoiceChannel(channelId))
            .VoiceParticipantLeft(targetUserId);
    }

    // -------------------------------------------------------------------------
    // WebRTC Signaling
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Called by a participant who just created an RTCSessionDescription (offer/answer).
    ///     Forwarded to a specific peer via their targetConnectionId so the hub acts as
    ///     a SignalR-based WebRTC signaling relay.
    /// </summary>
    public async Task SendWebRTCSignal(string targetConnectionId, object signal)
    {
        var senderId = GetUserId();
        var senderChannelId = voiceService.GetUserCurrentChannelId(senderId);

        if (!senderChannelId.HasValue)
        {
            await Clients.Caller.Error("You are not in a voice channel.");
            return;
        }

        // Forward to the specific target connection only
        await Clients.Client(targetConnectionId)
            .WebRTCSignal(senderId, signal);
    }

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Extracts the authenticated user's ID from the JWT claims.
    ///     The [Authorize] attribute guarantees this claim exists.
    ///     Throws if somehow missing — this is an infrastructure misconfiguration,
    ///     not a user error, so a hard throw is appropriate.
    /// </summary>
    private string GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException(
            "NameIdentifier claim is missing. Ensure JWT middleware is configured before SignalR.");

    /// <summary>
    ///     Extracts the courseId from the SignalR connection query string.
    ///     The client appends ?courseId=5 when initiating the connection.
    ///
    ///     ⚠️  Validated here but NOT trusted for authorization decisions.
    ///     Authorization uses the JWT userId cross-referenced against CourseUser in SQL.
    /// </summary>
    private int GetCourseId()
    {
        var raw = Context.GetHttpContext()?.Request.Query["courseId"].ToString();

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("A valid courseId query parameter is required to connect to CommunityHub.");

        var decodedArray = hashids.Decode(raw);
        if (decodedArray.Length > 0 && decodedArray[0] > 0)
        {
            return decodedArray[0];
        }

        if (int.TryParse(raw, out var courseId) && courseId > 0)
        {
            return courseId;
        }
        
        throw new InvalidOperationException(
            "A valid courseId query parameter is required to connect to CommunityHub.");
    }

    /// <summary>
    ///     Wraps the LastSeenAt SQL write in a try/catch so a DB failure
    ///     never propagates into the disconnect pipeline and crashes the Hub.
    ///
    ///     Uses IServiceScopeFactory to create an independent DI scope so the
    ///     DbContext lives as long as this background task — NOT the Hub's scope
    ///     (which is disposed after OnDisconnectedAsync returns).
    /// </summary>
    private async Task PersistLastSeenAtSafeAsync(string userId)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var scopedChatService = scope.ServiceProvider.GetRequiredService<IChatService>();
            await scopedChatService.PersistLastSeenAtAsync(userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist LastSeenAt for user {UserId}", userId);
        }
    }

    /// <summary>
    ///     Cleans up voice channel state on disconnect.
    ///     Fire-and-forget via _ = so it never blocks the disconnect pipeline.
    ///     Uses IServiceScopeFactory so the DbContext outlives the Hub's own scope.
    /// </summary>
    private async Task LeaveVoiceChannelSafeAsync(string userId)
    {
        try
        {
            var channelId = voiceService.GetUserCurrentChannelId(userId);
            if (!channelId.HasValue)
                return;

            // Ask voiceService to remove the participant
            await voiceService.LeaveVoiceAsync(userId);

            // Broadcast left to remaining peers in the voice room.
            // The SignalR group auto-removes the connection on disconnect,
            // so we only notify the remaining participants.
            await Clients
                .Group(HubGroups.VoiceChannel(channelId.Value))
                .VoiceParticipantLeft(userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clean up voice channel for user {UserId}", userId);
        }
    }
}
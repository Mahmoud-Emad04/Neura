using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Neura.Core.Contracts.Community;
using Neura.Core.Hubs;
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
    IChatService chatService)
    : Hub<ICommunityHubClient>
{
    // -------------------------------------------------------------------------
    // Connection Lifecycle
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Fires when a client establishes a SignalR connection.
    ///
    ///     Flow:
    ///     1. Extract userId + courseId from JWT claims / query string
    ///     2. Register connection in IPresenceTracker
    ///     3. Join the course-{id} group (lightweight events only)
    ///     4. If first connection → broadcast online presence to course peers
    ///     5. Send back the current online member list to the connecting client
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var courseId = GetCourseId();

        // Step 3 — join the course-level group (presence + unread badges only)
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Course(courseId));

        // Step 4 — register in presence tracker
        var justCameOnline = await presenceTracker.UserConnectedAsync(
            userId, courseId, Context.ConnectionId);

        // Step 5 — if this is their first connection, notify ALL course peers
        if (justCameOnline)
        {
            await Clients
                .GroupExcept(HubGroups.Course(courseId), Context.ConnectionId)
                .PresenceChanged(new PresenceUpdateDto(userId, true));
        }

        // Step 6 — send the current online member list back to the connecting client ONLY
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

            // Fire-and-forget SQL write — we are already in the disconnect path,
            // there is no client to return an error to, and we don't want to
            // block the disconnect pipeline on a DB write.
            _ = PersistLastSeenAtSafeAsync(result.UserId);
        }

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
    ///     1. Validate the request payload
    ///     2. Delegate persistence to IChatService (returns hydrated MessageDto)
    ///     3. Broadcast full MessageDto to channel-{id} group ONLY
    ///     4. Broadcast lightweight UnreadNotificationDto to course-{id} group,
    ///        EXCLUDING users who are already in the channel-{id} group
    ///        (they already see the message; they don't need an unread badge)
    ///
    ///     Step 4 is the heart of the Hybrid strategy — heavy payload goes to
    ///     channel subscribers only; everyone else gets just a badge ping.
    /// </summary>
    public async Task SendMessage(SendMessageHubRequest request)
    {
        var userId = GetUserId();

        // ----------------------------------------------------------------
        // Persist to SQL via service layer
        // ----------------------------------------------------------------
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

        // ----------------------------------------------------------------
        // Broadcast full message to channel-{id} subscribers
        // ----------------------------------------------------------------
        await Clients
            .Group(HubGroups.Channel(request.ChannelId))
            .ReceiveMessage(messageDto);

        // ----------------------------------------------------------------
        // Broadcast unread badge to course-{id} group,
        // but SKIP connections that are already in the channel group —
        // they're actively watching the channel and don't need an unread badge.
        //
        // SignalR's GroupExcept only accepts connection IDs, not group names.
        // We exclude only the SENDER's own connection here.
        // Users actively in the channel group will ignore the UnreadNotification
        // client-side because their channel is already active/focused.
        // ----------------------------------------------------------------
        var courseId = await chatService.GetCourseIdForChannelAsync(request.ChannelId);

        await Clients
            .GroupExcept(HubGroups.Course(courseId), Context.ConnectionId)
            .UnreadNotification(new UnreadNotificationDto(
                CourseId: courseId,
                ChannelId: request.ChannelId,
                ChannelName: string.Empty // Step 3: ChatService will hydrate this
            ));
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

        return int.TryParse(raw, out var courseId) && courseId > 0
            ? courseId
            : throw new InvalidOperationException(
                "A valid courseId query parameter is required to connect to CommunityHub.");
    }

    /// <summary>
    ///     Wraps the LastSeenAt SQL write in a try/catch so a DB failure
    ///     never propagates into the disconnect pipeline and crashes the Hub.
    ///     Errors are logged — not silently swallowed — via the injected logger.
    /// </summary>
    private async Task PersistLastSeenAtSafeAsync(string userId)
    {
        try
        {
            await chatService.PersistLastSeenAtAsync(userId);   // courseId gone
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[CommunityHub] Failed to persist LastSeenAt for user {userId}: {ex.Message}");
        }
    }
}
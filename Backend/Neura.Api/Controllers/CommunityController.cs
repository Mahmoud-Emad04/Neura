using Microsoft.AspNetCore.SignalR;
using Neura.Core.Contracts.Community;
using Neura.Core.Hubs;
using Neura.Services.Hubs;
using System.Security.Claims;

namespace Neura.Api.Controllers;

/// <summary>
///     REST endpoints that complement the SignalR Hub.
///
///     ┌─────────────────────────────────────────────────────────────────┐
///     │  Rule: SignalR = real-time events                               │
///     │        REST    = initial data load + CRUD that isn't real-time  │
///     └─────────────────────────────────────────────────────────────────┘
///
///     What lives here (not in the Hub):
///     ├── GET    channels           → initial sidebar load
///     ├── POST   channels           → create (broadcast ChannelCreated)
///     ├── PUT    channels/{id}      → update (broadcast ChannelUpdated)
///     ├── DELETE channels/{id}      → soft-delete (broadcast ChannelDeleted)
///     ├── PUT    channels/reorder   → drag-and-drop reorder
///     ├── GET    message history    → cursor-paginated scroll
///     ├── GET    course members     → member list panel hydration
///     ├── PATCH  message            → edit (broadcast MessageEdited)
///     └── DELETE message            → soft-delete (broadcast MessageDeleted)
/// </summary>
[ApiController]
[Authorize]
[Route("api/community")]
public sealed class CommunityController(
    IChatService chatService,
    IVoiceChannelService voiceService,
    IHubContext<CommunityHub, ICommunityHubClient> hubContext)
    : ControllerBase
{
    // =========================================================================
    // Channels — Read
    // =========================================================================

    /// <summary>
    ///     Returns all text + voice channels for a course, ordered by Position.
    ///     Called once when the user opens the course community page.
    ///     GET /api/community/courses/{courseId}/channels
    /// </summary>
    [HttpGet("courses/{courseId:int}/channels")]
    [ProducesResponseType(typeof(IReadOnlyList<ChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChannels(
        int courseId,
        CancellationToken ct = default)
    {
        try
        {
            var channels = await chatService.GetChannelsAsync(
                courseId, GetUserId(), ct);

            return Ok(channels);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // =========================================================================
    // Channels — Create
    // =========================================================================

    /// <summary>
    ///     Creates a new channel within a course.
    ///     Requires CourseRole.Level >= 3 (CoInstructor+) or platform Admin/SuperAdmin.
    ///     Broadcasts ChannelCreated to course group so all connected peers
    ///     see the new channel in their sidebar immediately.
    ///     POST /api/community/courses/{courseId}/channels
    /// </summary>
    [HttpPost("courses/{courseId:int}/channels")]
    [ProducesResponseType(typeof(ChannelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateChannel(
        int courseId,
        [FromBody] CreateChannelRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var channelDto = await chatService.CreateChannelAsync(
                courseId,
                GetUserId(),
                request.Name,
                request.Type,
                request.Topic,
                ct);

            // Broadcast to course group — sidebar updates in real time
            await hubContext.Clients
                .Group(HubGroups.Course(courseId))
                .ChannelCreated(channelDto);

            return CreatedAtAction(
                nameof(GetChannels),
                new { courseId },
                channelDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // =========================================================================
    // Channels — Update
    // =========================================================================

    /// <summary>
    ///     Updates an existing channel's name and topic.
    ///     Type and Position are immutable through this endpoint.
    ///     Requires CourseRole.Level >= 3 or platform Admin/SuperAdmin.
    ///     Broadcasts ChannelUpdated to course group.
    ///     PUT /api/community/channels/{channelId}
    /// </summary>
    [HttpPut("channels/{channelId:int}")]
    [ProducesResponseType(typeof(ChannelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateChannel(
        int channelId,
        [FromBody] UpdateChannelRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var channelDto = await chatService.UpdateChannelAsync(
                channelId,
                GetUserId(),
                request.Name,
                request.Topic,
                ct);

            // Resolve courseId for the broadcast group
            var courseId = await chatService.GetCourseIdForChannelAsync(channelId, ct);

            await hubContext.Clients
                .Group(HubGroups.Course(courseId))
                .ChannelUpdated(channelDto);

            return Ok(channelDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // =========================================================================
    // Channels — Delete
    // =========================================================================

    /// <summary>
    ///     Soft-deletes a channel. Messages within it are preserved
    ///     (accessible via admin queries).
    ///     Requires CourseRole.Level >= 3 or platform Admin/SuperAdmin.
    ///     Broadcasts ChannelDeleted to course group so peers remove it from sidebar.
    ///     DELETE /api/community/channels/{channelId}
    /// </summary>
    [HttpDelete("channels/{channelId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChannel(
        int channelId,
        CancellationToken ct = default)
    {
        try
        {
            var (deletedId, courseId) = await chatService.DeleteChannelAsync(
                channelId, GetUserId(), ct);

            // Broadcast to course group
            await hubContext.Clients
                .Group(HubGroups.Course(courseId))
                .ChannelDeleted(deletedId);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // =========================================================================
    // Channels — Reorder
    // =========================================================================

    /// <summary>
    ///     Bulk-reorders all channels in a course.
    ///     The client sends the complete ordered list of channel IDs
    ///     after a drag-and-drop operation.
    ///     Requires CourseRole.Level >= 3 or platform Admin/SuperAdmin.
    ///     PUT /api/community/courses/{courseId}/channels/reorder
    /// </summary>
    [HttpPut("courses/{courseId:int}/channels/reorder")]
    [ProducesResponseType(typeof(IReadOnlyList<ChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReorderChannels(
        int courseId,
        [FromBody] ReorderChannelsRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var reorderedChannels = await chatService.ReorderChannelsAsync(
                courseId, GetUserId(), request.ChannelIds, ct);

            return Ok(reorderedChannels);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // =========================================================================
    // Message History
    // =========================================================================

    /// <summary>
    ///     Cursor-paginated message history for a channel.
    ///
    ///     First load:  GET /api/community/channels/{channelId}/messages
    ///     Next page:   GET /api/community/channels/{channelId}/messages?before=1234&amp;pageSize=50
    ///
    ///     Response order: newest → oldest (client reverses for display).
    /// </summary>
    [HttpGet("channels/{channelId:int}/messages")]
    [ProducesResponseType(typeof(PagedMessagesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessages(
        int channelId,
        [FromQuery] long? before = null,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var result = await chatService.GetMessageHistoryAsync(
                channelId, GetUserId(), before, pageSize, ct);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // =========================================================================
    // Message Edit
    // =========================================================================

    /// <summary>
    ///     Edits a message and broadcasts the change to the channel group via SignalR.
    ///     PATCH /api/community/messages/{messageId}
    /// </summary>
    [HttpPatch("messages/{messageId:long}")]
    [ProducesResponseType(typeof(MessageEditedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditMessage(
        long messageId,
        [FromBody] EditMessageRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var editedDto = await chatService.EditMessageAsync(
                messageId, GetUserId(), request.NewContent, ct);

            // Broadcast to channel group — same real-time event the Hub would send
            await hubContext.Clients
                .Group(HubGroups.Channel(editedDto.ChannelId))
                .MessageEdited(editedDto);

            return Ok(editedDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // =========================================================================
    // Message Delete
    // =========================================================================

    /// <summary>
    ///     Soft-deletes a message and broadcasts the tombstone to the channel group.
    ///     DELETE /api/community/messages/{messageId}
    /// </summary>
    [HttpDelete("messages/{messageId:long}")]
    [ProducesResponseType(typeof(MessageDeletedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage(
        long messageId,
        CancellationToken ct = default)
    {
        try
        {
            var deletedDto = await chatService.DeleteMessageAsync(
                messageId, GetUserId(), ct);

            // Broadcast tombstone to channel group
            await hubContext.Clients
                .Group(HubGroups.Channel(deletedDto.ChannelId))
                .MessageDeleted(deletedDto);

            return Ok(deletedDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // =========================================================================
    // Voice Channel Participants (REST initial load)
    // =========================================================================

    /// <summary>
    ///     Returns the current participant list for a voice channel.
    ///     Called when a user opens the voice channel panel to hydrate
    ///     the participant list without a SignalR round-trip.
    ///     GET /api/community/channels/{channelId}/voice-participants
    /// </summary>
    [HttpGet("channels/{channelId:int}/voice-participants")]
    [ProducesResponseType(typeof(IReadOnlyList<VoiceParticipantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVoiceParticipants(
        int channelId,
        CancellationToken ct = default)
    {
        try
        {
            var participants = await voiceService.GetParticipantsAsync(
                channelId, GetUserId());
            return Ok(participants);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // =========================================================================
    // Course Members
    // =========================================================================

    /// <summary>
    ///     Returns all course members with their real-time online status.
    ///     Hydrated by cross-referencing IPresenceTracker (no extra SQL for IsOnline).
    ///     GET /api/community/courses/{courseId}/members
    /// </summary>
    [HttpGet("courses/{courseId:int}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCourseMembers(
        int courseId,
        CancellationToken ct = default)
    {
        try
        {
            var members = await chatService.GetCourseMembersAsync(
                courseId, GetUserId(), ct);

            return Ok(members);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("NameIdentifier claim missing.");
}

// ─────────────────────────────────────────────────────────────────────────────
// Request models (inline — too small to warrant separate files)
// ─────────────────────────────────────────────────────────────────────────────

public sealed record EditMessageRequest(
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(1)]
    [System.ComponentModel.DataAnnotations.MaxLength(4_000)]
    string NewContent
);
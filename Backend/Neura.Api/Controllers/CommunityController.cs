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
///     ├── GET channels         → initial sidebar load
///     ├── GET message history  → cursor-paginated scroll
///     ├── GET course members   → member list panel hydration
///     ├── PATCH message        → edit (triggers Hub broadcast via IHubContext)
///     └── DELETE message       → soft-delete (triggers Hub broadcast via IHubContext)
/// </summary>
[ApiController]
[Authorize]
[Route("api/community")]
public sealed class CommunityController(
    IChatService chatService,
    IHubContext<CommunityHub, ICommunityHubClient> hubContext)
    : ControllerBase
{
    // -------------------------------------------------------------------------
    // Channels
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Returns all text + voice channels for a course, ordered by Position.
    ///     Called once when the user opens the course community page.
    ///     GET /api/community/courses/{courseId}/channels
    /// </summary>
    [HttpGet("courses/{courseId:int}/channels")]
    [ProducesResponseType(typeof(IReadOnlyList<ChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    // -------------------------------------------------------------------------
    // Message History
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Message Edit
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Message Delete
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Course Members
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

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
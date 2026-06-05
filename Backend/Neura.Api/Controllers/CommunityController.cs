using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.Community.CreateChannel;
using Neura.Api.Features.Community.DeleteChannel;
using Neura.Api.Features.Community.DeleteMessage;
using Neura.Api.Features.Community.EditMessage;
using Neura.Api.Features.Community.GetChannels;
using Neura.Api.Features.Community.GetCourseMembers;
using Neura.Api.Features.Community.GetMessageHistory;
using Neura.Api.Features.Community.GetVoiceParticipants;
using Neura.Api.Features.Community.ReorderChannels;
using Neura.Api.Features.Community.UpdateChannel;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/community")]
public sealed class CommunityController(ISender sender) : ControllerBase
{
    [HttpGet("courses/{courseId:int}/channels")]
    [ProducesResponseType(typeof(IReadOnlyList<ChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChannels(int courseId, CancellationToken ct = default)
    {
        try
        {
            var query = new GetChannelsQuery(courseId, User.GetUserId()!);
            var result = await sender.Send(query, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost("courses/{courseId:int}/channels")]
    [ProducesResponseType(typeof(ChannelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateChannel(int courseId, [FromBody] CreateChannelRequest request, CancellationToken ct = default)
    {
        try
        {
            var command = new CreateChannelCommand(courseId, request, User.GetUserId()!);
            var channelDto = await sender.Send(command, ct);
            return CreatedAtAction(nameof(GetChannels), new { courseId }, channelDto);
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

    [HttpPut("channels/{channelId:int}")]
    [ProducesResponseType(typeof(ChannelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateChannel(int channelId, [FromBody] UpdateChannelRequest request, CancellationToken ct = default)
    {
        try
        {
            var command = new UpdateChannelCommand(channelId, request, User.GetUserId()!);
            var channelDto = await sender.Send(command, ct);
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

    [HttpDelete("channels/{channelId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChannel(int channelId, CancellationToken ct = default)
    {
        try
        {
            var command = new DeleteChannelCommand(channelId, User.GetUserId()!);
            await sender.Send(command, ct);
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

    [HttpPut("courses/{courseId:int}/channels/reorder")]
    [ProducesResponseType(typeof(IReadOnlyList<ChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReorderChannels(int courseId, [FromBody] ReorderChannelsRequest request, CancellationToken ct = default)
    {
        try
        {
            var command = new ReorderChannelsCommand(courseId, request, User.GetUserId()!);
            var reorderedChannels = await sender.Send(command, ct);
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

    [HttpGet("channels/{channelId:int}/messages")]
    [ProducesResponseType(typeof(PagedMessagesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessages(int channelId, [FromQuery] long? before = null, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        try
        {
            var query = new GetMessageHistoryQuery(channelId, User.GetUserId()!, before, pageSize);
            var result = await sender.Send(query, ct);
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

    [HttpPatch("messages/{messageId:long}")]
    [ProducesResponseType(typeof(MessageEditedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditMessage(long messageId, [FromBody] EditMessageRequest request, CancellationToken ct = default)
    {
        try
        {
            var command = new EditMessageCommand(messageId, request, User.GetUserId()!);
            var editedDto = await sender.Send(command, ct);
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

    [HttpDelete("messages/{messageId:long}")]
    [ProducesResponseType(typeof(MessageDeletedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage(long messageId, CancellationToken ct = default)
    {
        try
        {
            var command = new DeleteMessageCommand(messageId, User.GetUserId()!);
            var deletedDto = await sender.Send(command, ct);
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

    [HttpGet("channels/{channelId:int}/voice-participants")]
    [ProducesResponseType(typeof(IReadOnlyList<VoiceParticipantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVoiceParticipants(int channelId, CancellationToken ct = default)
    {
        try
        {
            var query = new GetVoiceParticipantsQuery(channelId, User.GetUserId()!);
            var participants = await sender.Send(query, ct);
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

    [HttpGet("courses/{courseId:int}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCourseMembers(int courseId, CancellationToken ct = default)
    {
        try
        {
            var query = new GetCourseMembersQuery(courseId, User.GetUserId()!);
            var members = await sender.Send(query, ct);
            return Ok(members);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }
}
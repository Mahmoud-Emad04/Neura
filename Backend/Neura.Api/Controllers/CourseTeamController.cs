using MediatR;
using Neura.Api.Features.CourseTeam.CancelInvitation;
using Neura.Api.Features.CourseTeam.ChangeTeamRole;
using Neura.Api.Features.CourseTeam.GetPendingInvitations;
using Neura.Api.Features.CourseTeam.GetTeamMember;
using Neura.Api.Features.CourseTeam.GetTeamMembers;
using Neura.Api.Features.CourseTeam.GetTeamOverview;
using Neura.Api.Features.CourseTeam.InviteTeamMember;
using Neura.Api.Features.CourseTeam.RemoveTeamMember;
using Neura.Api.Features.CourseTeam.ResendInvitation;
using Neura.Api.Features.CourseTeam.TransferOwnership;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/team")]
[Authorize]
public class CourseTeamController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Get team overview including members and pending invitations
    /// </summary>
    [HttpGet]
    [HasCoursePermission(CoursePermission.ViewAnalytics)]
    [ProducesResponseType(typeof(TeamOverviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamOverview(int courseId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetTeamOverviewQuery(courseId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Get all team members
    /// </summary>
    [HttpGet("members")]
    [HasCoursePermission(CoursePermission.ViewContent)]
    [ProducesResponseType(typeof(List<TeamMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamMembers(int courseId, CancellationToken ct)
    {
        var query = new GetTeamMembersQuery(courseId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Get specific team member
    /// </summary>
    [HttpGet("members/{userId}")]
    [HasCoursePermission(CoursePermission.ViewAnalytics)]
    [ProducesResponseType(typeof(TeamMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamMember(int courseId, string userId, CancellationToken ct)
    {
        var query = new GetTeamMemberQuery(courseId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Invite a new team member
    /// </summary>
    [HttpPost("invite")]
    [HasCoursePermission(CoursePermission.ManageTeam)]
    [ProducesResponseType(typeof(CourseInvitationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteTeamMember(int courseId, [FromBody] InviteTeamMemberRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new InviteTeamMemberCommand(courseId, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPendingInvitations), new { courseId }, result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Remove a team member
    /// </summary>
    [HttpDelete("members/{userId}")]
    [HasCoursePermission(CoursePermission.ManageTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTeamMember(int courseId, string userId, CancellationToken ct)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new RemoveTeamMemberCommand(courseId, userId, requesterId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Change a team member's role
    /// </summary>
    [HttpPatch("members/{userId}/role")]
    [HasCoursePermission(CoursePermission.ManageTeam)]
    [ProducesResponseType(typeof(TeamMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeTeamRole(
        int courseId,
        string userId,
        [FromBody] ChangeTeamRoleRequest request,
        CancellationToken ct)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new ChangeTeamRoleCommand(courseId, userId, request, requesterId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Transfer course ownership
    /// </summary>
    [HttpPost("transfer")]
    [HasCoursePermission(CoursePermission.TransferOwnership)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferOwnership(int courseId, [FromBody] TransferOwnershipRequest request, CancellationToken ct)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new TransferOwnershipCommand(courseId, request, requesterId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(new { message = "Ownership transferred successfully" })
            : result.ToProblem();
    }

    /// <summary>
    ///     Get pending invitations for course
    /// </summary>
    [HttpGet("invitations")]
    [HasCoursePermission(CoursePermission.ManageTeam)]
    [ProducesResponseType(typeof(List<CourseInvitationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPendingInvitations(int courseId, CancellationToken ct)
    {
        var query = new GetPendingInvitationsQuery(courseId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Cancel a pending invitation
    /// </summary>
    [HttpDelete("invitations/{invitationId:int}")]
    [HasCoursePermission(CoursePermission.ManageTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInvitation(int courseId, int invitationId, CancellationToken ct)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new CancelInvitationCommand(courseId, invitationId, requesterId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Resend a pending invitation
    /// </summary>
    [HttpPost("invitations/{invitationId:int}/resend")]
    [HasCoursePermission(CoursePermission.ManageTeam)]
    [ProducesResponseType(typeof(CourseInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendInvitation(int courseId, int invitationId, CancellationToken ct)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new ResendInvitationCommand(courseId, invitationId, requesterId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
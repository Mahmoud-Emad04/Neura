using System.Security.Claims;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/team")]
[Authorize]
public class CourseTeamController : ControllerBase
{
    private readonly ICourseTeamService _teamService;

    public CourseTeamController(ICourseTeamService teamService)
    {
        _teamService = teamService;
    }

    /// <summary>
    ///     Get team overview including members and pending invitations
    /// </summary>
    [HttpGet]
    [HasCoursePermission(CoursePermission.ViewAnalytics)]
    [ProducesResponseType(typeof(TeamOverviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamOverview(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _teamService.GetTeamOverviewAsync(courseId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Get all team members
    /// </summary>
    [HttpGet("members")]
    [HasCoursePermission(CoursePermission.ViewContent)]
    [ProducesResponseType(typeof(List<TeamMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamMembers(int courseId)
    {
        var result = await _teamService.GetTeamMembersAsync(courseId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Get specific team member
    /// </summary>
    [HttpGet("members/{userId}")]
    [HasCoursePermission(CoursePermission.ViewAnalytics)]
    [ProducesResponseType(typeof(TeamMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamMember(int courseId, string userId)
    {
        var result = await _teamService.GetTeamMemberAsync(courseId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Invite a new team member
    /// </summary>
    [HttpPost("invite")]
    [HasCoursePermission(CoursePermission.ManageTeam)]
    [ProducesResponseType(typeof(CourseInvitationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteTeamMember(int courseId, [FromBody] InviteTeamMemberRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _teamService.InviteTeamMemberAsync(courseId, request, userId);

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
    public async Task<IActionResult> RemoveTeamMember(int courseId, string userId)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _teamService.RemoveTeamMemberAsync(courseId, userId, requesterId);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
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
        [FromBody] ChangeTeamRoleRequest request)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _teamService.ChangeTeamRoleAsync(courseId, userId, request, requesterId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Transfer course ownership
    /// </summary>
    [HttpPost("transfer")]
    [HasCoursePermission(CoursePermission.TransferOwnership)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferOwnership(int courseId, [FromBody] TransferOwnershipRequest request)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _teamService.TransferOwnershipAsync(courseId, request, requesterId);

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
    public async Task<IActionResult> GetPendingInvitations(int courseId)
    {
        var result = await _teamService.GetPendingInvitationsAsync(courseId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Cancel a pending invitation
    /// </summary>
    [HttpDelete("invitations/{invitationId:int}")]
    [HasCoursePermission(CoursePermission.ManageTeam)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInvitation(int courseId, int invitationId)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _teamService.CancelInvitationAsync(courseId, invitationId, requesterId);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    /// <summary>
    ///     Resend a pending invitation
    /// </summary>
    [HttpPost("invitations/{invitationId:int}/resend")]
    [HasCoursePermission(CoursePermission.ManageTeam)]
    [ProducesResponseType(typeof(CourseInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendInvitation(int courseId, int invitationId)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _teamService.ResendInvitationAsync(courseId, invitationId, requesterId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}
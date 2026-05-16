using System.Security.Claims;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly ICourseTeamService _teamService;

    public InvitationsController(ICourseTeamService teamService)
    {
        _teamService = teamService;
    }

    /// <summary>
    ///     Get invitation details by token (public - for viewing invitation before login)
    /// </summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InvitationDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvitationByToken(string token)
    {
        var result = await _teamService.GetInvitationByTokenAsync(token);

        if (result.IsFailure) return NotFound(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    ///     Accept an invitation (requires authentication)
    /// </summary>
    [HttpPost("{token}/accept")]
    [Authorize]
    [ProducesResponseType(typeof(TeamMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvitation(string token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _teamService.AcceptInvitationAsync(token, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Reject an invitation (can be anonymous or authenticated)
    /// </summary>
    [HttpPost("{token}/reject")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectInvitation(string token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _teamService.RejectInvitationAsync(token, userId);

        return result.IsSuccess
            ? Ok(new { message = "Invitation rejected successfully" })
            : result.ToProblem();
    }

    /// <summary>
    ///     Get my pending invitations (authenticated user)
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(MyInvitationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyInvitations()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(userEmail)) return Ok(new MyInvitationsResponse());

        var result = await _teamService.GetMyInvitationsAsync(userEmail);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}
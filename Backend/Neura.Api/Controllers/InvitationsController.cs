using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Neura.Core.Contracts.CourseTeam;
using Neura.Api.Extensions;
using Neura.Api.Features.Invitations.AcceptInvitation;
using Neura.Api.Features.Invitations.GetInvitationByToken;
using Neura.Api.Features.Invitations.GetMyInvitations;
using Neura.Api.Features.Invitations.RejectInvitation;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/invitations")]
public class InvitationsController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Get invitation details by token (public - for viewing invitation before login)
    /// </summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InvitationDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvitationByToken(string token, CancellationToken ct)
    {
        var query = new GetInvitationByTokenQuery(token);
        var result = await sender.Send(query, ct);

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
    public async Task<IActionResult> AcceptInvitation(string token, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new AcceptInvitationCommand(token, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Reject an invitation (can be anonymous or authenticated)
    /// </summary>
    [HttpPost("{token}/reject")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectInvitation(string token, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var command = new RejectInvitationCommand(token, userId);
        var result = await sender.Send(command, ct);

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
    public async Task<IActionResult> GetMyInvitations(CancellationToken ct)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(userEmail)) return Ok(new MyInvitationsResponse());

        var query = new GetMyInvitationsQuery(userEmail);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
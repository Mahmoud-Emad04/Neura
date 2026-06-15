using MediatR;
using Neura.Api.Features.InstructorApplications.ApproveApplication;
using Neura.Api.Features.InstructorApplications.GetApplicationById;
using Neura.Api.Features.InstructorApplications.GetApplications;
using Neura.Api.Features.InstructorApplications.GetMyApplicationStatus;
using Neura.Api.Features.InstructorApplications.SubmitApplication;
using Neura.Api.Features.InstructorApplications.UpdateApplication;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Enums;
using Neura.Core.InstructorApplication;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/instructor")]
[Authorize]
public class InstructorApplicationController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Submit a new instructor application
    /// </summary>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitApplication([FromBody] SubmitApplicationRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new SubmitApplicationCommand(request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMyApplicationStatus), result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Get current user's application status
    /// </summary>
    [HttpGet("application")]
    [ProducesResponseType(typeof(MyApplicationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyApplicationStatus(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetMyApplicationStatusQuery(userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Update pending application
    /// </summary>
    [HttpPut("application")]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApplication([FromBody] UpdateApplicationRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new UpdateApplicationCommand(request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    // Admin Operations
    // ==========================================

    [HttpGet("applications")]
    [AdminOnly]
    public async Task<IActionResult> GetApplications(
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = new GetApplicationsQuery(status, page, pageSize);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("applications/{id:int}")]
    [AdminOnly]
    public async Task<IActionResult> GetApplicationById(int id, CancellationToken ct)
    {
        var query = new GetApplicationByIdQuery(id);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("applications/{id:int}/approve")]
    [AdminOnly]
    public async Task<IActionResult> ApproveApplication(int id, CancellationToken ct)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new ApproveApplicationCommand(id, reviewerId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    //[HttpPost("applications/{id:int}/reject")]
    //[AdminOnly]
    //public async Task<IActionResult> RejectApplication(
    //    int id,
    //    [FromBody] RejectApplicationRequest request,
    //    CancellationToken ct)
    //{
    //    var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    //    var command = new RejectApplicationCommand(id, request.Reason, reviewerId);
    //    var result = await sender.Send(command, ct);

    //    return result.IsSuccess ? Ok() : result.ToProblem();
    //}
}
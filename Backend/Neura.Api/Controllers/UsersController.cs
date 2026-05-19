using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Neura.Core.Contracts.Instructor;
using Neura.Api.Extensions;
using Neura.Api.Features.Users.GetInstructorByCourseId;
using Neura.Core.Abstractions;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Get Instructor By CourseId
    /// </summary>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns></returns>
    [ProducesResponseType(typeof(InstructorSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpGet("course/{courseId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInstructorByCourseId(string courseId, CancellationToken ct)
    {
        var query = new GetInstructorByCourseIdQuery(courseId);
        var response = await sender.Send(query, ct);
        return response.IsSuccess ? Ok(response.Value) : response.ToProblem();
    }
}
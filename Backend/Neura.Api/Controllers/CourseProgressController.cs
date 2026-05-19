using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Neura.Api.Extensions;
using Neura.Core.Contracts.Lessons;
using Neura.Api.Features.CourseProgress.GetCourseProgress;
using Neura.Api.Features.CourseProgress.GetNextLesson;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CourseProgressController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     GET api/CourseProgress/{keyId}
    ///     Returns the current user's progress in this course.
    /// </summary>
    [HttpGet("{keyId}")]
    [ProducesResponseType(typeof(CourseProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgress(
        [FromRoute] string keyId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetCourseProgressQuery(keyId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     GET api/CourseProgress/{keyId}/next-lesson
    ///     Returns the next lesson the user should view.
    /// </summary>
    [HttpGet("{keyId}/next-lesson")]
    [ProducesResponseType(typeof(NextLessonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNextLesson(
        [FromRoute] string keyId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetNextLessonQuery(keyId, userId);
        var result = await sender.Send(query, ct);

        if (result.IsFailure)
            return result.ToProblem();

        return result.Value is null ? NoContent() : Ok(result.Value);
    }
}

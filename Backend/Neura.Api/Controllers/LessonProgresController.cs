using Neura.Api.Extensions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LessonProgresController(ILessonProgressService lessonProgressService) : ControllerBase
{
    private readonly ILessonProgressService _lessonProgressService = lessonProgressService;

    /// <summary>
    ///     Marks a lesson as completed for the current user.
    ///     Called by the frontend when video reaches threshold OR article is read.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    [HttpPost("lessons/{lessonId:int}/complete")]
    [ProducesResponseType(typeof(LessonCompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkLessonCompleted(
        [FromRoute] int lessonId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var result = await _lessonProgressService
            .MarkLessonCompletedAsync(lessonId, userId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Gets the current user's progress summary for a specific course.
    ///     Includes total/completed lesson counts, percentage, next lesson, and completed IDs.
    /// </summary>
    [HttpGet("courses/{keyId}")]
    [ProducesResponseType(typeof(CourseProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseProgress(
        [FromRoute] string keyId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var result = await _lessonProgressService
            .GetCourseProgressAsync(keyId, userId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Gets the next lesson the user should view in a given course.
    ///     Returns null if the course is fully completed.
    /// </summary>
    [HttpGet("courses/{keyId}/next-lesson")]
    [ProducesResponseType(typeof(NextLessonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNextLesson(
        [FromRoute] string keyId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;

        var result = await _lessonProgressService
            .GetNextLessonAsync(keyId, userId, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        // No next lesson means course is completed
        return result.Value is null
            ? NoContent()
            : Ok(result.Value);
    }
}

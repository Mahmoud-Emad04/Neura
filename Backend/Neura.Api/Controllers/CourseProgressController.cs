using Neura.Api.Extensions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CourseProgressController(ILessonProgressService lessonProgressService) : ControllerBase
{
    private readonly ILessonProgressService _lessonProgressService = lessonProgressService;

    /// <summary>
    ///     GET api/courses/{keyId}/progress
    ///     Returns the current user's progress in this course.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CourseProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgress(
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
    ///     GET api/courses/{keyId}/progress/next-lesson
    ///     Returns the next lesson the user should view.
    /// </summary>
    [HttpGet("next-lesson")]
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

        return result.Value is null
            ? NoContent()
            : Ok(result.Value);
    }
}

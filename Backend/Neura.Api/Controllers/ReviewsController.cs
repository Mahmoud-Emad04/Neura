using Neura.Api.Extensions;
using Neura.Core.Contracts;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReviewsController(ICourseService courseService) : ControllerBase
{
    private readonly ICourseService _courseService = courseService;

    /// <summary>
    ///     Adds or updates a review for a specific course.
    ///     Route: POST /api/Review/courses/{courseId}
    /// </summary>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="request">The rating (1-5) and optional comment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>NoContent on success, or an error result.</returns>
    [HttpPost("~/api/courses/{courseId}/reviews")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddReview(string courseId, [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.AddReviewAsync(courseId, User.GetUserId()!, request, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
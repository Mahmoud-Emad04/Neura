using Neura.Api.Extensions;
using Neura.Core.Contracts.Review;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReviewsController(ICourseService courseService, IReviewService reviewService) : ControllerBase
{
    private readonly ICourseService _courseService = courseService;
    private readonly IReviewService _reviewService = reviewService;

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

    [HttpGet("course/{courseId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(
        [FromRoute] string courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5)
    {
        var result = await _reviewService.CourseReviewsAsync(courseId, page, pageSize, CancellationToken.None);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
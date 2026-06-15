using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.Reviews.AddReview;
using Neura.Api.Features.Reviews.GetReviews;
using Neura.Core.Contracts.Review;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReviewsController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Adds or updates a review for a specific course.
    ///     Route: POST /api/courses/{courseId}/reviews
    /// </summary>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="request">The rating (1-5) and optional comment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>NoContent on success, or an error result.</returns>
    [HttpPost("~/api/courses/{courseId}/reviews")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddReview(string courseId, [FromBody] ReviewRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new AddReviewCommand(courseId, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpGet("course/{courseId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(
        [FromRoute] string courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken ct = default)
    {
        var query = new GetReviewsQuery(courseId, page, pageSize);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
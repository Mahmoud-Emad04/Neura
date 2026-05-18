using MediatR;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Review;
using System.Security.Claims;

namespace Neura.Api.Features.Reviews.AddReview;

public sealed class AddReviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("courses/{courseId}/reviews", async (
            string courseId,
            [FromBody] ReviewRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new AddReviewCommand(courseId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Reviews")
        .WithName("AddReview");
    }
}

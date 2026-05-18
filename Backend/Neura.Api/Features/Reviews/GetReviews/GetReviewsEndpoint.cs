using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Review;

namespace Neura.Api.Features.Reviews.GetReviews;

public sealed class GetReviewsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/reviews/course/{courseId}", async (
            string courseId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 5 : pageSize;

            var query = new GetReviewsQuery(courseId, page, pageSize);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .WithTags("Reviews")
        .WithName("GetReviews");
    }
}

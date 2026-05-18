using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.CourseProgress.GetNextLesson;

public sealed class GetNextLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/{keyId}/progress/next-lesson", async (
            string keyId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var query = new GetNextLessonQuery(keyId, userId);
            var result = await sender.Send(query, ct);

            if (result.IsFailure)
                return result.ToProblemMinimal();

            return result.Value is null
                ? Results.NoContent()
                : Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .WithTags("CourseProgress")
        .WithName("GetNextLesson");
    }
}

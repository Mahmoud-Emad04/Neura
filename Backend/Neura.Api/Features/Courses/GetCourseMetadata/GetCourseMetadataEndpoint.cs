using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.Courses.GetCourseMetadata;

public sealed class GetCourseMetadataEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/courses/{courseId}/metadata", async (
            string courseId,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.Identity?.IsAuthenticated == true 
                ? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                : null;

            var query = new GetCourseMetadataQuery(courseId, userId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .WithTags("Courses")
        .WithName("GetCourseMetadata")
        .AllowAnonymous();
    }
}

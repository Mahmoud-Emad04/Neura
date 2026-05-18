using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.Enrollment.Enroll;

public sealed class EnrollEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("courses/{courseId}/enroll", async (
            string courseId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new EnrollCommand(courseId, userId);

            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.CreatedAtRoute(
                    routeName: "GetEnrollmentStatus",
                    routeValues: new { courseId },
                    value: result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Enrollment")
        .WithName("Enroll");
    }
}

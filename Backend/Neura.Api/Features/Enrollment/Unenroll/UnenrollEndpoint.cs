using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.Enrollment.Unenroll;

public sealed class UnenrollEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/courses/{courseId:int}/unenroll", async (
            int courseId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new UnenrollCommand(courseId, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(new { message = "Successfully unenrolled from course" }) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Enrollment")
        .WithName("Unenroll");
    }
}

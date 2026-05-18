using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;
using System.Security.Claims;

namespace Neura.Api.Features.Courses.CreateCourse;

public sealed class CreateCourseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/courses", async (
            [FromForm] CourseRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new CreateCourseCommand(request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.CreatedAtRoute("GetCourseMetadata", new { courseId = result.Value.KeyId }, result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithTags("Courses")
        .WithName("CreateCourse");
    }
}

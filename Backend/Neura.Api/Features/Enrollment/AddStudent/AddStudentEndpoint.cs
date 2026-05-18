using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Enrollment;
using System.Security.Claims;

namespace Neura.Api.Features.Enrollment.AddStudent;

public sealed class AddStudentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/courses/{courseId:int}/students", async (
            int courseId,
            AddStudentRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new AddStudentCommand(courseId, userId, request.Email);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.CreatedAtRoute(
                    routeName: "GetCourseStudents",
                    routeValues: new { courseId },
                    value: result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Enrollment")
        .WithName("AddStudent");
    }
}

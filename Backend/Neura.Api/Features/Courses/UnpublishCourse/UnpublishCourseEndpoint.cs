// ═══════════════════════════════════════════════════════════════════════════
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by CoursesController (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ═══════════════════════════════════════════════════════════════════════════

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;
//using Neura.Core.Abstractions;
//using System.Security.Claims;

//namespace Neura.Api.Features.Courses.UnpublishCourse;

//public sealed class UnpublishCourseEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPost("api/courses/{courseId}/unpublish", async (
//            string courseId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new UnpublishCourseCommand(courseId, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess
//                ? Results.Ok(result.Value)
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization("CoursePermission_ManageSettings")
//        .WithTags("Courses")
//        .WithName("UnpublishCourse");
//    }
//}

// ═══════════════════════════════════════════════════════════════════════════
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by CoursesController (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ═══════════════════════════════════════════════════════════════════════════

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;
//using Neura.Core.Abstractions;
//using Neura.Core.Contracts.Courses;
//using System.Security.Claims;

//namespace Neura.Api.Features.Courses.UpdateCourseDetails;

//public sealed class UpdateCourseDetailsEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPut("api/courses/{courseId}", async (
//            string courseId,
//            [FromForm] CourseUpdateRequest request,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new UpdateCourseDetailsCommand(courseId, request, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess
//                ? Results.NoContent()
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization("CoursePermission_EditContent")
//        .DisableAntiforgery()
//        .WithTags("Courses")
//        .WithName("UpdateCourseDetails");
//    }
//}

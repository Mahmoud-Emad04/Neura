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

//namespace Neura.Api.Features.Courses.GetCourseMetadata;

//public sealed class GetCourseMetadataEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/courses/{courseId}/metadata", async (
//            string courseId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

//            var query = new GetCourseMetadataQuery(courseId, userId);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess
//                ? Results.Ok(result.Value)
//                : result.ToProblemMinimal();
//        })
//        .AllowAnonymous()
//        .WithTags("Courses")
//        .WithName("GetCourseMetadata");
//    }
//}

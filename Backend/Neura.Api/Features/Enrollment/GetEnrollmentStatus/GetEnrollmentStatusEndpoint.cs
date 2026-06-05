// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;
//using Neura.Core.Abstractions;
//using Neura.Core.Contracts.Enrollment;
//using System.Security.Claims;

//namespace Neura.Api.Features.Enrollment.GetEnrollmentStatus;

//public sealed class GetEnrollmentStatusEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/courses/{courseId}/enrollment-status", async (
//            string courseId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

//            if (string.IsNullOrEmpty(userId))
//            {
//                return Results.Ok(new EnrollmentStatusResponse
//                {
//                    IsEnrolled = false,
//                    CanEnroll = false,
//                    CannotEnrollReason = "Please sign in to enroll",
//                    CourseId = courseId
//                });
//            }

//            var query = new GetEnrollmentStatusQuery(courseId, userId);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .WithTags("Enrollment")
//        .WithName("GetEnrollmentStatus")
//        .AllowAnonymous();
//    }
//}

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
//using System.Security.Claims;

//namespace Neura.Api.Features.Enrollment.RemoveStudent;

//public sealed class RemoveStudentEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapDelete("api/courses/{courseId:int}/students/{studentId}", async (
//            int courseId,
//            string studentId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new RemoveStudentCommand(courseId, studentId, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.NoContent() 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("Enrollment")
//        .WithName("RemoveStudent");
//    }
//}

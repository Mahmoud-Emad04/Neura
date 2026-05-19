// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Extensions;
//using Neura.Api.Infrastructure;
//using System.Security.Claims;

//namespace Neura.Api.Features.ExamAnalytics.GetStudentAttemptDetail;

//public sealed class GetStudentAttemptDetailEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/exams/{examId:int}/analytics/attempts/{attemptId:int}", async (
//            int examId,
//            int attemptId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.GetUserId()!;
//            var result = await sender.Send(new GetStudentAttemptDetailQuery(examId, attemptId, userId), ct);
//            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("Exam Analytics")
//        .WithName("GetStudentAttemptDetail");
//    }
//}

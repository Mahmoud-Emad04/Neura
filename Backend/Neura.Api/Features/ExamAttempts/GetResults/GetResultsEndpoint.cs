// ═══════════════════════════════════════════════════════════════════════════
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by ExamAttemptsController (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ═══════════════════════════════════════════════════════════════════════════

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;
//using Neura.Core.Abstractions;
//using System.Security.Claims;

//namespace Neura.Api.Features.ExamAttempts.GetResults;

//public sealed class GetResultsEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/exam-attempts/{attemptId:int}/results", async (
//            int attemptId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var query = new GetResultsQuery(attemptId, userId);
//            var result = await sender.Send(query, ct);

//            return result.IsSuccess
//                ? Results.Ok(result.Value)
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("ExamAttempts")
//        .WithName("GetResults");
//    }
//}

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

//namespace Neura.Api.Features.ExamQuestions.DeleteQuestion;

//public sealed class DeleteQuestionEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapDelete("api/exams/{lessonId:int}/questions/{questionId:int}", async (
//            int questionId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new DeleteQuestionCommand(questionId, userId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.Ok() 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization(policy => policy.RequireClaim("CoursePermission", "EditContent"))
//        .WithTags("ExamQuestions")
//        .WithName("DeleteQuestion");
//    }
//}

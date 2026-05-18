using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Question;
using System.Security.Claims;

namespace Neura.Api.Features.ExamQuestions.UpdateQuestion;

public sealed class UpdateQuestionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/exams/{lessonId:int}/questions/{questionId:int}", async (
            int questionId,
            [FromBody] UpdateQuestionRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new UpdateQuestionCommand(questionId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization(policy => policy.RequireClaim("CoursePermission", "EditContent"))
        .WithTags("ExamQuestions")
        .WithName("UpdateQuestion");
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.ExamAttempt;
using System.Security.Claims;

namespace Neura.Api.Features.ExamAttempts.SaveAnswer;

public sealed class SaveAnswerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/exam-attempts/{attemptId:int}/answers/{questionId:int}", async (
            int attemptId,
            int questionId,
            [FromBody] SaveAnswerRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new SaveAnswerCommand(attemptId, questionId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("ExamAttempts")
        .WithName("SaveAnswer");
    }
}

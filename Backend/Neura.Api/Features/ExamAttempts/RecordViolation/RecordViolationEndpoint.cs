using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.ExamAttempt;
using System.Security.Claims;

namespace Neura.Api.Features.ExamAttempts.RecordViolation;

public sealed class RecordViolationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("exam-attempts/{attemptId:int}/violations", async (
            int attemptId,
            [FromBody] ViolationRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new RecordViolationCommand(attemptId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("ExamAttempts")
        .WithName("RecordViolation");
    }
}

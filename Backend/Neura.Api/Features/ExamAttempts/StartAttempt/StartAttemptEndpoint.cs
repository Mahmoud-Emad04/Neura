using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.ExamAttempts.StartAttempt;

public sealed class StartAttemptEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/exam-attempts/exam/{lessonId:int}/start", async (
            int lessonId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new StartAttemptCommand(lessonId, userId);
            var result = await sender.Send(command, ct);

            if (result.IsSuccess)
                return Results.Created($"/api/exam-attempts/{result.Value.AttemptId}/resume", result.Value);

            return result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("ExamAttempts")
        .WithName("StartAttempt");
    }
}

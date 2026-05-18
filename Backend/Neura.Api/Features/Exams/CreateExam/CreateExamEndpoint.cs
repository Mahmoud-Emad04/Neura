using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Exam;
using System.Security.Claims;

namespace Neura.Api.Features.Exams.CreateExam;

public sealed class CreateExamEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("exams", async (
            [FromBody] CreateExamRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new CreateExamCommand(request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.CreatedAtRoute("GetExamById", new { lessonId = result.Value.LessonId }, result.Value)
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Exams")
        .WithName("CreateExam");
    }
}

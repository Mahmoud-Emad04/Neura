using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Question;
using System.Security.Claims;

namespace Neura.Api.Features.ExamQuestions.AddQuestion;

public sealed class AddQuestionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("exams/{lessonId:int}/questions", async (
            int lessonId,
            [FromBody] CreateQuestionRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new AddQuestionCommand(lessonId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.CreatedAtRoute("AddQuestion", new { lessonId = result.Value.Id }, result.Value)
                : result.ToProblemMinimal();
        })
        .RequireAuthorization(policy => policy.RequireClaim("CoursePermission", "EditContent"))
        .WithTags("ExamQuestions")
        .WithName("AddQuestion");
    }
}

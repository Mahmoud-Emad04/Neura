using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.Exams.DeleteExam;

public sealed class DeleteExamEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("exams/{lessonId:int}", async (
            int lessonId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new DeleteExamCommand(lessonId, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization(policy => policy.RequireClaim("CoursePermission", "EditContent"))
        .WithTags("Exams")
        .WithName("DeleteExam");
    }
}

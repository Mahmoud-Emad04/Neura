using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.Exams.PublishExam;

public sealed class PublishExamEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("exams/{lessonId:int}/publish", async (
            int lessonId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new PublishExamCommand(lessonId, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization(policy => policy.RequireClaim("CoursePermission", "EditContent"))
        .WithTags("Exams")
        .WithName("PublishExam");
    }
}

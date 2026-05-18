using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.ExamAttempts.GetExamInfo;

public sealed class GetExamInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("exam-attempts/exam/{lessonId:int}/info", async (
            int lessonId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var query = new GetExamInfoQuery(lessonId, userId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("ExamAttempts")
        .WithName("GetExamInfo");
    }
}

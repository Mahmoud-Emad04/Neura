using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Extensions;
using Neura.Api.Infrastructure;
using System.Security.Claims;

namespace Neura.Api.Features.ExamAnalytics.GetScoreDistribution;

public sealed class GetScoreDistributionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("exams/{examId:int}/analytics/score-distribution", async (
            int examId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId()!;
            var result = await sender.Send(new GetScoreDistributionQuery(examId, userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Exam Analytics")
        .WithName("GetScoreDistribution");
    }
}

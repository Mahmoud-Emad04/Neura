using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Extensions;
using Neura.Api.Infrastructure;
using System.Security.Claims;

namespace Neura.Api.Features.ExamAnalytics.GetStudentAttempts;

public sealed class GetStudentAttemptsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/exams/{examId:int}/analytics/attempts", async (
            int examId,
            int? page,
            int? pageSize,
            string? sortBy,
            bool? descending,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId()!;
            var query = new GetStudentAttemptsQuery(
                examId, userId,
                page ?? 1, pageSize ?? 20,
                sortBy, descending ?? true);
            var result = await sender.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Exam Analytics")
        .WithName("GetStudentAttempts");
    }
}

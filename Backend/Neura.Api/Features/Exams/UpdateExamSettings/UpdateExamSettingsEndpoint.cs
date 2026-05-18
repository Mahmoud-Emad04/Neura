using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Exam;
using System.Security.Claims;

namespace Neura.Api.Features.Exams.UpdateExamSettings;

public sealed class UpdateExamSettingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("exams/{lessonId:int}/settings", async (
            int lessonId,
            [FromBody] UpdateExamSettingsRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new UpdateExamSettingsCommand(lessonId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization(policy => policy.RequireClaim("CoursePermission", "EditContent"))
        .WithTags("Exams")
        .WithName("UpdateExamSettings");
    }
}

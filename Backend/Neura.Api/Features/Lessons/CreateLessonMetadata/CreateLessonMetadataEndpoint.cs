using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;
using System.Security.Claims;

namespace Neura.Api.Features.Lessons.CreateLessonMetadata;

public sealed class CreateLessonMetadataEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lessons/{sectionId:int}/init", async (
            int sectionId,
            [FromBody] CreateLessonRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new CreateLessonMetadataCommand(sectionId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(new { LessonId = result.Value }) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization("SectionPermission_EditContent")
        .WithTags("Lessons")
        .WithName("InitializeLesson");
    }
}

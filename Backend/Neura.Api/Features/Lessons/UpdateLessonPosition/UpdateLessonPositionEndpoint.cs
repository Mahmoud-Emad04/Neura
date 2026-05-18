using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;
using System.Security.Claims;

namespace Neura.Api.Features.Lessons.UpdateLessonPosition;

public sealed class UpdateLessonPositionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/lessons/{id:int}/position", async (
            int id,
            [FromBody] UpdateLessonPositionRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new UpdateLessonPositionCommand(id, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.NoContent() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization("LessonPermission_EditContent")
        .WithTags("Lessons")
        .WithName("UpdateLessonPosition");
    }
}

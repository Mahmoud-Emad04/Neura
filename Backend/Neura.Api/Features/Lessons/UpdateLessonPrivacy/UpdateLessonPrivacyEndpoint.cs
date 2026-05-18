using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;
using System.Security.Claims;

namespace Neura.Api.Features.Lessons.UpdateLessonPrivacy;

public sealed class UpdateLessonPrivacyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/lessons/{id:int}/privacy", async (
            int id,
            [FromBody] UpdateLessonPrivacyRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new UpdateLessonPrivacyCommand(id, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.NoContent() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization("LessonPermission_EditContent")
        .WithTags("Lessons")
        .WithName("UpdateLessonPrivacy");
    }
}

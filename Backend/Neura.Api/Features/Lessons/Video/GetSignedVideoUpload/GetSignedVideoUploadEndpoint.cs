using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.Lessons.Video.GetSignedVideoUpload;

public sealed class GetSignedVideoUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lessons/{id:int}/video/signed-upload", async (
            int id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new GetSignedVideoUploadCommand(id, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization("LessonPermission_EditContent")
        .WithTags("Lessons")
        .WithName("GetSignedVideoUploadCredentials");
    }
}

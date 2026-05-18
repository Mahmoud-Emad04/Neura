using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Files;
using System.Security.Claims;

namespace Neura.Api.Features.Announcements.UpdatePostCommentImage;

public sealed class UpdatePostCommentImageEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("announcements/comments/{commentId:int}/image", async (
            int commentId,
            [FromForm] UploadImageRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            var command = new UpdatePostCommentImageCommand(commentId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.NoContent() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithTags("Announcements")
        .WithName("UpdatePostCommentImage");
    }
}

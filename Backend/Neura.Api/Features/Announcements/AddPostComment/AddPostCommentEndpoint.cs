using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Announcement;
using System.Security.Claims;

namespace Neura.Api.Features.Announcements.AddPostComment;

public sealed class AddPostCommentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/announcements/posts/{postId:int}/comments", async (
            int postId,
            [FromForm] PostCommentRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            var command = new AddPostCommentCommand(postId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.CreatedAtRoute("GetPostById", new { postId }, result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithTags("Announcements")
        .WithName("AddPostComment");
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using System.Security.Claims;

namespace Neura.Api.Features.Announcements.TogglePostLike;

public sealed class TogglePostLikeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("announcements/posts/{postId:int}/likes", async (
            int postId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            var command = new TogglePostLikeCommand(postId, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.NoContent() 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Announcements")
        .WithName("TogglePostLike");
    }
}

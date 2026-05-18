using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using System.Security.Claims;

namespace Neura.Api.Features.Announcements.GetPostById;

public sealed class GetPostByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("announcements/posts/{postId:int}", async (
            int postId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var query = new GetPostByIdQuery(postId, userId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .WithTags("Announcements")
        .WithName("GetPostById");
    }
}

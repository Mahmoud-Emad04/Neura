using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Announcement;
using System.Security.Claims;

namespace Neura.Api.Features.Announcements.UpdatePostComment;

public sealed class UpdatePostCommentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/announcements/comments/{commentId:int}", async (
            int commentId,
            [FromBody] PostCommentUpdateRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            var command = new UpdatePostCommentCommand(commentId, request, userId);
            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Announcements")
        .WithName("UpdatePostComment");
    }
}

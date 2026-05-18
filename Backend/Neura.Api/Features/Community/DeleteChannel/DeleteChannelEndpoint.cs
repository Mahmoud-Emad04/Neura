using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using System.Security.Claims;

namespace Neura.Api.Features.Community.DeleteChannel;

public sealed class DeleteChannelEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("community/channels/{channelId:int}", async (
            int channelId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var command = new DeleteChannelCommand(channelId, userId);
                await sender.Send(command, ct);
                
                return Results.NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        })
        .RequireAuthorization()
        .WithTags("Community")
        .WithName("DeleteChannel");
    }
}

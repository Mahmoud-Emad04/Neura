using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using System.Security.Claims;

namespace Neura.Api.Features.Community.GetVoiceParticipants;

public sealed class GetVoiceParticipantsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("community/channels/{channelId:int}/voice-participants", async (
            int channelId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var query = new GetVoiceParticipantsQuery(channelId, userId);
                var result = await sender.Send(query, ct);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .RequireAuthorization()
        .WithTags("Community")
        .WithName("GetVoiceParticipants");
    }
}

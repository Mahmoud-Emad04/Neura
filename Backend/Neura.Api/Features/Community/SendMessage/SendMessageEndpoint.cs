using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using System.Security.Claims;

namespace Neura.Api.Features.Community.SendMessage;

public sealed class SendMessageEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/community/channels/{channelId:int}/messages", async (
            int channelId,
            SendMessageRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var command = new SendMessageCommand(
                channelId, userId, request.Content, request.ReplyToMessageId);

            var result = await sender.Send(command, ct);

            return result.IsSuccess 
                ? Results.CreatedAtRoute(
                    routeName: "GetMessageHistory",
                    routeValues: new { channelId },
                    value: new SendMessageResponse(result.Value.Id, result.Value.SentAt))
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Community");
    }
}

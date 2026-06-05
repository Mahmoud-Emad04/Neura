using MediatR;
using Microsoft.AspNetCore.SignalR;
using Neura.Core.Hubs;
using Neura.Services.Hubs;

namespace Neura.Api.Features.Community.DeleteChannel;

internal sealed class DeleteChannelHandler(
    IChatService chatService,
    IHubContext<CommunityHub, ICommunityHubClient> hubContext) 
    : IRequestHandler<DeleteChannelCommand>
{
    public async Task Handle(
        DeleteChannelCommand command, CancellationToken ct)
    {
        var (deletedId, courseId) = await chatService.DeleteChannelAsync(
            command.ChannelId, command.UserId, ct);

        await hubContext.Clients
            .Group(HubGroups.Course(courseId))
            .ChannelDeleted(deletedId);
    }
}

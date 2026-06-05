using MediatR;
using Microsoft.AspNetCore.SignalR;
using Neura.Core.Contracts.Community;
using Neura.Core.Hubs;
using Neura.Services.Hubs;

namespace Neura.Api.Features.Community.DeleteMessage;

internal sealed class DeleteMessageHandler(
    IChatService chatService,
    IHubContext<CommunityHub, ICommunityHubClient> hubContext) 
    : IRequestHandler<DeleteMessageCommand, MessageDeletedDto>
{
    public async Task<MessageDeletedDto> Handle(
        DeleteMessageCommand command, CancellationToken ct)
    {
        var deletedDto = await chatService.DeleteMessageAsync(
            command.MessageId, command.UserId, ct);

        await hubContext.Clients
            .Group(HubGroups.Channel(deletedDto.ChannelId))
            .MessageDeleted(deletedDto);

        return deletedDto;
    }
}

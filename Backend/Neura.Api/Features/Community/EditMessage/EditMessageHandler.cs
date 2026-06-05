using MediatR;
using Microsoft.AspNetCore.SignalR;
using Neura.Core.Contracts.Community;
using Neura.Core.Hubs;
using Neura.Services.Hubs;

namespace Neura.Api.Features.Community.EditMessage;

internal sealed class EditMessageHandler(
    IChatService chatService,
    IHubContext<CommunityHub, ICommunityHubClient> hubContext) 
    : IRequestHandler<EditMessageCommand, MessageEditedDto>
{
    public async Task<MessageEditedDto> Handle(
        EditMessageCommand command, CancellationToken ct)
    {
        var editedDto = await chatService.EditMessageAsync(
            command.MessageId, command.UserId, command.Request.NewContent, ct);

        await hubContext.Clients
            .Group(HubGroups.Channel(editedDto.ChannelId))
            .MessageEdited(editedDto);

        return editedDto;
    }
}

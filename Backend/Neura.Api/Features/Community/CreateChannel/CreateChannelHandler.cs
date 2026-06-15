using MediatR;
using Microsoft.AspNetCore.SignalR;
using Neura.Core.Contracts.Community;
using Neura.Core.Hubs;
using Neura.Services.Hubs;

namespace Neura.Api.Features.Community.CreateChannel;

internal sealed class CreateChannelHandler(
    IChatService chatService,
    IHubContext<CommunityHub, ICommunityHubClient> hubContext)
    : IRequestHandler<CreateChannelCommand, ChannelDto>
{
    public async Task<ChannelDto> Handle(
        CreateChannelCommand command, CancellationToken ct)
    {
        var channelDto = await chatService.CreateChannelAsync(
            command.CourseId,
            command.UserId,
            command.Request.Name,
            command.Request.Type,
            command.Request.Topic,
            ct);

        await hubContext.Clients
            .Group(HubGroups.Course(command.CourseId))
            .ChannelCreated(channelDto);

        return channelDto;
    }
}

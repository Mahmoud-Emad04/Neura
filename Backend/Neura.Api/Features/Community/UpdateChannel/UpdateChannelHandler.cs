using MediatR;
using Microsoft.AspNetCore.SignalR;
using Neura.Core.Contracts.Community;
using Neura.Core.Hubs;
using Neura.Services.Hubs;

namespace Neura.Api.Features.Community.UpdateChannel;

internal sealed class UpdateChannelHandler(
    IChatService chatService,
    IHubContext<CommunityHub, ICommunityHubClient> hubContext) 
    : IRequestHandler<UpdateChannelCommand, ChannelDto>
{
    public async Task<ChannelDto> Handle(
        UpdateChannelCommand command, CancellationToken ct)
    {
        var channelDto = await chatService.UpdateChannelAsync(
            command.ChannelId,
            command.UserId,
            command.Request.Name,
            command.Request.Topic,
            ct);

        var courseId = await chatService.GetCourseIdForChannelAsync(command.ChannelId, ct);

        await hubContext.Clients
            .Group(HubGroups.Course(courseId))
            .ChannelUpdated(channelDto);

        return channelDto;
    }
}

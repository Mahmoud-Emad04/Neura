using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.ReorderChannels;

internal sealed class ReorderChannelsHandler(IChatService chatService) 
    : IRequestHandler<ReorderChannelsCommand, IReadOnlyList<ChannelDto>>
{
    public async Task<IReadOnlyList<ChannelDto>> Handle(
        ReorderChannelsCommand command, CancellationToken ct)
    {
        return await chatService.ReorderChannelsAsync(
            command.CourseId, command.UserId, command.Request.ChannelIds, ct);
    }
}

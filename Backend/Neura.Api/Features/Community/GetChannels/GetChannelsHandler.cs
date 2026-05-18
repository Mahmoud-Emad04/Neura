using MediatR;
using Neura.Core.Contracts.Community;
using Neura.Core.Hubs;

namespace Neura.Api.Features.Community.GetChannels;

internal sealed class GetChannelsHandler(IChatService chatService) 
    : IRequestHandler<GetChannelsQuery, IReadOnlyList<ChannelDto>>
{
    public async Task<IReadOnlyList<ChannelDto>> Handle(
        GetChannelsQuery query, CancellationToken ct)
    {
        return await chatService.GetChannelsAsync(query.CourseId, query.UserId, ct);
    }
}

using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.GetChannels;

public sealed record GetChannelsQuery(int CourseId, string UserId)
    : IRequest<IReadOnlyList<ChannelDto>>;

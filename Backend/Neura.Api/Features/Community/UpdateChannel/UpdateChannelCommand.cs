using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.UpdateChannel;

public sealed record UpdateChannelCommand(int ChannelId, UpdateChannelRequest Request, string UserId)
    : IRequest<ChannelDto>;

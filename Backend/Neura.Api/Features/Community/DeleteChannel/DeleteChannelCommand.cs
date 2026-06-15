using MediatR;

namespace Neura.Api.Features.Community.DeleteChannel;

public sealed record DeleteChannelCommand(int ChannelId, string UserId)
    : IRequest;

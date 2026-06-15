using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.SendMessage;

public sealed record SendMessageCommand(
    int ChannelId, string UserId, string Content, long? ReplyToMessageId)
    : IRequest<Result<MessageDto>>;

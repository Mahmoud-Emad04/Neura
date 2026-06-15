using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.DeleteMessage;

public sealed record DeleteMessageCommand(long MessageId, string UserId)
    : IRequest<MessageDeletedDto>;

using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.EditMessage;

public sealed record EditMessageRequest(string NewContent);

public sealed record EditMessageCommand(long MessageId, EditMessageRequest Request, string UserId) 
    : IRequest<MessageEditedDto>;

using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.GetMessageHistory;

public sealed record GetMessageHistoryQuery(
    int ChannelId, string UserId, long? BeforeMessageId, int PageSize) 
    : IRequest<Result<PagedMessagesDto>>;

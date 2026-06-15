using MediatR;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.BulkUpdateTagsOrder;

public sealed record BulkUpdateTagsOrderCommand(BulkUpdateTagsOrderRequest Request, string UserId)
    : IRequest<Result>;

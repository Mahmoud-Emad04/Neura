using MediatR;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.BulkToggleTagsActive;

public sealed record BulkToggleTagsActiveCommand(BulkToggleTagsActiveRequest Request, string UserId)
    : IRequest<Result>;

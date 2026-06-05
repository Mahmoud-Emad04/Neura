using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.BulkDeleteTags;

public sealed record BulkDeleteTagsCommand(BulkDeleteTagsRequest Request, bool Force, string UserId) 
    : IRequest<Result>;

using MediatR;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.GetTags;

public sealed record GetTagsQuery(TagFilters Filters)
    : IRequest<Result<TagListResponse>>;

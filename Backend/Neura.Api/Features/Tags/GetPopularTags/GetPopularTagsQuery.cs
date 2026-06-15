using MediatR;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.GetPopularTags;

public sealed record GetPopularTagsQuery(int Count)
    : IRequest<Result<IEnumerable<TagSummaryResponse>>>;

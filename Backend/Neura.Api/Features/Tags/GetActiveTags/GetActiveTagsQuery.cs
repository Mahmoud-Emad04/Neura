using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.GetActiveTags;

public sealed record GetActiveTagsQuery() 
    : IRequest<Result<IEnumerable<TagSummaryResponse>>>;

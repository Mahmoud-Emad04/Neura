using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.GetTagBySlug;

public sealed record GetTagBySlugQuery(string Slug) 
    : IRequest<Result<TagResponse>>;

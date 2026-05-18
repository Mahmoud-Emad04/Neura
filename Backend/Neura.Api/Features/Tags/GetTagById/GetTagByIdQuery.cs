using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.GetTagById;

public sealed record GetTagByIdQuery(int Id) 
    : IRequest<Result<TagResponse>>;

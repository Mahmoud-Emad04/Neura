using MediatR;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.CreateTag;

public sealed record CreateTagCommand(CreateTagRequest Request, string UserId)
    : IRequest<Result<TagResponse>>;

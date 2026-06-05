using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.UpdateTag;

public sealed record UpdateTagCommand(int Id, UpdateTagRequest Request, string UserId) 
    : IRequest<Result<TagResponse>>;

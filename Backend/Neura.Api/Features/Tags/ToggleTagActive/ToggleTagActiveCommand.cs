using MediatR;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Features.Tags.ToggleTagActive;

public sealed record ToggleTagActiveCommand(int Id, string UserId)
    : IRequest<Result<TagResponse>>;

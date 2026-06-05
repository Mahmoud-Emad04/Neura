using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Announcements.TogglePostVisibility;

public sealed record TogglePostVisibilityCommand(int PostId, string UserId) 
    : IRequest<Result>;

using MediatR;

namespace Neura.Api.Features.Announcements.TogglePostVisibility;

public sealed record TogglePostVisibilityCommand(int PostId, string UserId)
    : IRequest<Result>;

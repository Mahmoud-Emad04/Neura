using MediatR;

namespace Neura.Api.Features.Announcements.TogglePostLike;

public sealed record TogglePostLikeCommand(int PostId, string UserId)
    : IRequest<Result>;

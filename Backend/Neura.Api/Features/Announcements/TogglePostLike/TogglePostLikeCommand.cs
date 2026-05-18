using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Announcements.TogglePostLike;

public sealed record TogglePostLikeCommand(int PostId, string UserId) 
    : IRequest<Result>;

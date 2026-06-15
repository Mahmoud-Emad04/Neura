using MediatR;

namespace Neura.Api.Features.Announcements.RemovePostComment;

public sealed record RemovePostCommentCommand(int CommentId, string UserId)
    : IRequest<Result>;

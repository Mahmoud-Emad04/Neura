using MediatR;
using Neura.Core.Contracts.Announcement;

namespace Neura.Api.Features.Announcements.UpdatePostComment;

public sealed record UpdatePostCommentCommand(int CommentId, PostCommentUpdateRequest Request, string UserId)
    : IRequest<Result<PostCommentResponse>>;

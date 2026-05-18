using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Announcements.RemovePostComment;

public sealed record RemovePostCommentCommand(int CommentId, string UserId) 
    : IRequest<Result>;

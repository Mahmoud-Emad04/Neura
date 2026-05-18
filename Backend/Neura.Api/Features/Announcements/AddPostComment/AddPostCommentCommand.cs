using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Announcement;

namespace Neura.Api.Features.Announcements.AddPostComment;

public sealed record AddPostCommentCommand(int PostId, PostCommentRequest Request, string UserId) 
    : IRequest<Result<PostCommentResponse>>;

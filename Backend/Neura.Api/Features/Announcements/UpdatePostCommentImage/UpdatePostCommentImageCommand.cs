using MediatR;
using Neura.Core.Contracts.Files;

namespace Neura.Api.Features.Announcements.UpdatePostCommentImage;

public sealed record UpdatePostCommentImageCommand(int CommentId, UploadImageRequest Request, string UserId)
    : IRequest<Result>;

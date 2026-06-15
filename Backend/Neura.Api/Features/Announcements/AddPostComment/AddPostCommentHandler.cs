using MediatR;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Errors;
using Neura.Core.FilesConsts;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.AddPostComment;

internal sealed class AddPostCommentHandler(
    ApplicationDbContext context,
    IFileService fileService,
    IServiceHelpers helpers)
    : IRequestHandler<AddPostCommentCommand, Result<PostCommentResponse>>
{
    public async Task<Result<PostCommentResponse>> Handle(
        AddPostCommentCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentInvalidData);

        var postExists = await context.Posts
            .AnyAsync(p => p.Id == command.PostId && !p.IsDeleted, ct);

        if (!postExists)
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.PostNotFound);

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await context.PostComments
                .AnyAsync(c => c.Id == request.ParentCommentId.Value
                               && !c.IsDeleted
                               && c.PostId == command.PostId, ct);

            if (!parentExists)
                return Result.Failure<PostCommentResponse>(AnnouncementErrors.ParentCommentNotFound);
        }

        var comment = new PostComment
        {
            PostId = command.PostId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content,
            CreatedById = command.UserId,
            CreatedOn = DateTime.UtcNow
        };

        if (request.Image is not null)
            comment.ImageUrl = await fileService.UploadImageAsync(
                request.Image, ImageConsts.PostComment, ct);

        context.PostComments.Add(comment);
        await context.SaveChangesAsync(ct);

        var baseUrl = helpers.GetBaseUrl();
        var projection = await AnnouncementHelpers.ProjectComments(
                context.PostComments.AsNoTracking().Where(c => c.Id == comment.Id))
            .FirstAsync(ct);

        var response = AnnouncementHelpers.MapCommentProjectionToResponse(
            projection,
            AnnouncementHelpers.EmptyRepliesLookup,
            baseUrl,
            command.UserId);

        return Result.Success(response);
    }
}

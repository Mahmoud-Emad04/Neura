using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.UpdatePostComment;

internal sealed class UpdatePostCommentHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<UpdatePostCommentCommand, Result<PostCommentResponse>>
{
    public async Task<Result<PostCommentResponse>> Handle(
        UpdatePostCommentCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentInvalidData);

        var comment = await context.PostComments
            .FirstOrDefaultAsync(c => c.Id == command.CommentId && !c.IsDeleted, ct);

        if (comment is null)
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentNotFound);

        var isAdmin = helpers.IsUserInRole(DefaultRoles.Admin) || helpers.IsUserInRole(DefaultRoles.SuperAdmin);

        if (comment.CreatedById != command.UserId && !isAdmin)
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentAccessDenied);

        comment.Content = request.Content;
        comment.UpdatedOn = DateTime.UtcNow;
        comment.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        var baseUrl = helpers.GetBaseUrl();

        var commentAndReplies = await AnnouncementHelpers.ProjectComments(
                context.PostComments
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted && (c.Id == command.CommentId || c.ParentCommentId == command.CommentId)))
            .ToListAsync(ct);

        var root = commentAndReplies.First(c => c.Id == command.CommentId);
        var repliesLookup = AnnouncementHelpers.BuildRepliesLookup(commentAndReplies);
        var response = AnnouncementHelpers.MapCommentProjectionToResponse(root, repliesLookup, baseUrl, command.UserId);

        return Result.Success(response);
    }
}

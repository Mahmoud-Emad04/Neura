using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.RemovePostComment;

internal sealed class RemovePostCommentHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers) 
    : IRequestHandler<RemovePostCommentCommand, Result>
{
    public async Task<Result> Handle(
        RemovePostCommentCommand command, CancellationToken ct)
    {
        var comment = await context.PostComments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == command.CommentId, ct);

        if (comment is null)
            return Result.Failure(AnnouncementErrors.CommentNotFound);

        var isAdmin = helpers.IsUserInRole(DefaultRoles.Admin) || helpers.IsUserInRole(DefaultRoles.SuperAdmin);

        if (comment.CreatedById == command.UserId || isAdmin || comment.Post.CreatedById == command.UserId)
        {
            comment.IsDeleted = true;
            comment.DeletedOn = DateTime.UtcNow;
            comment.DeletedById = command.UserId;

            await context.SaveChangesAsync(ct);
            return Result.Success();
        }

        return Result.Failure(AnnouncementErrors.CommentAccessDenied);
    }
}

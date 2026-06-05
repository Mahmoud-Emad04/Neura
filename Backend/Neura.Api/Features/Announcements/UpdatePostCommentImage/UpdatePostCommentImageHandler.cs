using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Errors;
using Neura.Core.FilesConsts;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.UpdatePostCommentImage;

internal sealed class UpdatePostCommentImageHandler(
    ApplicationDbContext context,
    IFileService fileService,
    IServiceHelpers helpers) 
    : IRequestHandler<UpdatePostCommentImageCommand, Result>
{
    public async Task<Result> Handle(
        UpdatePostCommentImageCommand command, CancellationToken ct)
    {
        var comment = await context.PostComments
            .FirstOrDefaultAsync(c => c.Id == command.CommentId && !c.IsDeleted, ct);

        if (comment is null)
            return Result.Failure(AnnouncementErrors.CommentNotFound);

        var isAdmin = helpers.IsUserInRole(DefaultRoles.Admin) || helpers.IsUserInRole(DefaultRoles.SuperAdmin);

        if (comment.CreatedById != command.UserId && !isAdmin)
            return Result.Failure(AnnouncementErrors.CommentAccessDenied);

        if (!string.IsNullOrEmpty(comment.ImageUrl))
            fileService.Delete(comment.ImageUrl);

        comment.ImageUrl = await fileService.UploadImageAsync(
            command.Request.Image, ImageConsts.PostComment, ct);
        comment.UpdatedOn = DateTime.UtcNow;
        comment.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

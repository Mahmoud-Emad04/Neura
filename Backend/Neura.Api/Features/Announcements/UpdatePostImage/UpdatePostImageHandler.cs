using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Errors;
using Neura.Core.FilesConsts;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.UpdatePostImage;

internal sealed class UpdatePostImageHandler(
    ApplicationDbContext context,
    IFileService fileService,
    IServiceHelpers helpers)
    : IRequestHandler<UpdatePostImageCommand, Result>
{
    public async Task<Result> Handle(
        UpdatePostImageCommand command, CancellationToken ct)
    {
        var post = await context.Posts
            .FirstOrDefaultAsync(p => p.Id == command.PostId && !p.IsDeleted, ct);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        var isAdmin = helpers.IsUserInRole(DefaultRoles.Admin) || helpers.IsUserInRole(DefaultRoles.SuperAdmin);

        if (post.CreatedById != command.UserId && !isAdmin)
            return Result.Failure(AnnouncementErrors.PostAccessDenied);

        if (!string.IsNullOrEmpty(post.ImageUrl))
            fileService.Delete(post.ImageUrl);

        post.ImageUrl = await fileService.UploadImageAsync(command.Request.Image, ImageConsts.Post, ct);
        post.UpdatedOn = DateTime.UtcNow;
        post.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.RemovePost;

internal sealed class RemovePostHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<RemovePostCommand, Result>
{
    public async Task<Result> Handle(
        RemovePostCommand command, CancellationToken ct)
    {
        var post = await context.Posts
            .FirstOrDefaultAsync(p => p.Id == command.PostId, ct);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        var isAdmin = helpers.IsUserInRole(DefaultRoles.Admin) || helpers.IsUserInRole(DefaultRoles.SuperAdmin);

        if (post.CreatedById != command.UserId && !isAdmin)
            return Result.Failure(AnnouncementErrors.PostAccessDenied);

        post.IsDeleted = true;
        post.DeletedOn = DateTime.UtcNow;
        post.DeletedById = command.UserId;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

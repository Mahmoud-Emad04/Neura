using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.TogglePostVisibility;

internal sealed class TogglePostVisibilityHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<TogglePostVisibilityCommand, Result>
{
    public async Task<Result> Handle(
        TogglePostVisibilityCommand command, CancellationToken ct)
    {
        var post = await context.Posts
            .FirstOrDefaultAsync(p => p.Id == command.PostId && !p.IsDeleted, ct);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        var isAdmin = helpers.IsUserInRole(DefaultRoles.Admin) || helpers.IsUserInRole(DefaultRoles.SuperAdmin);

        if (post.CreatedById != command.UserId && !isAdmin)
            return Result.Failure(AnnouncementErrors.PostAccessDenied);

        post.IsPublic = !post.IsPublic;
        post.UpdatedOn = DateTime.UtcNow;
        post.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

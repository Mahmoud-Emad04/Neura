using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Entities;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Announcements.TogglePostLike;

internal sealed class TogglePostLikeHandler(ApplicationDbContext context) 
    : IRequestHandler<TogglePostLikeCommand, Result>
{
    public async Task<Result> Handle(
        TogglePostLikeCommand command, CancellationToken ct)
    {
        var postExists = await context.Posts
            .AnyAsync(p => p.Id == command.PostId && !p.IsDeleted, ct);

        if (!postExists)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        var existingLike = await context.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == command.PostId && l.UserId == command.UserId, ct);

        if (existingLike is not null)
        {
            context.PostLikes.Remove(existingLike);
        }
        else
        {
            context.PostLikes.Add(new PostLike
            {
                PostId = command.PostId,
                UserId = command.UserId,
                CreatedOn = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

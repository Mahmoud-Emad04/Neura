using MediatR;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.ToggleCourseBookmark;

internal sealed class ToggleCourseBookmarkHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<ToggleCourseBookmarkCommand, Result>
{
    public async Task<Result> Handle(
        ToggleCourseBookmarkCommand command, CancellationToken ct)
    {
        if (!TryDecodeCourseId(command.CourseIdKey, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        var bookmark = await context.CourseBookmarks
            .FirstOrDefaultAsync(cb => cb.CourseId == courseId && cb.UserId == command.UserId, ct);

        if (bookmark is null)
        {
            await context.CourseBookmarks.AddAsync(
                new CourseBookmark
                {
                    CourseId = courseId,
                    UserId = command.UserId,
                    IsDeleted = false,
                    CreatedOn = DateTime.UtcNow
                },
                ct);
        }
        else
        {
            bookmark.IsDeleted = !bookmark.IsDeleted;
        }

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }
        courseId = numbers[0];
        return true;
    }
}

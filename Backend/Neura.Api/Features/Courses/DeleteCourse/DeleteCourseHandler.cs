using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Neura.Api.Infrastructure;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.DeleteCourse;

internal sealed class DeleteCourseHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers,
    HybridCache hybridCache)
    : IRequestHandler<DeleteCourseCommand, Result>
{
    public async Task<Result> Handle(
        DeleteCourseCommand command, CancellationToken ct)
    {
        if (!TryDecodeCourseId(command.CourseIdKey, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        var course = await context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, ct);

        if (course is null)
            return Result.Failure(CourseErrors.CourseNotFound);

        if (course.IsDeleted)
            return Result.Success();

        course.IsDeleted = true;
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        // Invalidate course caches
        await hybridCache.RemoveAsync(CacheKeys.CourseFullContent, ct);

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

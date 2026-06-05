using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.CourseProgress.GetCourseProgress;

internal sealed class GetCourseProgressHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers) 
    : IRequestHandler<GetCourseProgressQuery, Result<CourseProgressResponse>>
{
    public async Task<Result<CourseProgressResponse>> Handle(
        GetCourseProgressQuery query, CancellationToken ct)
    {
        var courseKeyId = query.CourseKeyId;
        var userId = query.UserId;

        if (!TryDecodeCourseId(courseKeyId, out var courseId))
            return Result.Failure<CourseProgressResponse>(LessonProgressErrors.CourseNotFound);

        var lessons = await GetOrderedAccessibleLessonsAsync(courseId, userId, ct);

        if (lessons.Count == 0)
        {
            return Result.Success(new CourseProgressResponse(
                courseKeyId, 0, 0, 0, false, null, []));
        }

        var lessonIds = lessons.Select(l => l.Id).ToList();

        var completedIds = await context.LessonCompletions
            .AsNoTracking()
            .Where(lc => lc.UserId == userId && lessonIds.Contains(lc.LessonId) && !lc.IsDeleted)
            .Select(lc => lc.LessonId)
            .ToHashSetAsync(ct);

        var completedCount = completedIds.Count;
        var totalCount = lessons.Count;
        var percentage = (int)Math.Round((double)completedCount / totalCount * 100);
        var isCourseCompleted = completedCount == totalCount;

        NextLessonResponse? next = null;
        if (!isCourseCompleted)
        {
            var nextLesson = lessons.First(l => !completedIds.Contains(l.Id));
            next = new NextLessonResponse(
                nextLesson.Id,
                nextLesson.SectionId,
                nextLesson.Title,
                nextLesson.Type.ToString(),
                nextLesson.OrderIndex);
        }

        return Result.Success(new CourseProgressResponse(
            courseKeyId,
            totalCount,
            completedCount,
            percentage,
            isCourseCompleted,
            next,
            completedIds.ToList()));
    }

    private async Task<List<LessonOrderInfo>> GetOrderedAccessibleLessonsAsync(
        int courseId, string userId, CancellationToken ct)
    {
        var isEnrolled = await context.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu => cu.CourseId == courseId && cu.UserId == userId && !cu.IsDeleted,
                ct);

        var dbQuery = context.Lessons
            .AsNoTracking()
            .Where(l =>
                l.Section.CourseId == courseId &&
                !l.IsDeleted &&
                !l.Section.IsDeleted &&
                l.IsPublished);

        if (!isEnrolled)
            dbQuery = dbQuery.Where(l => l.IsPreview);

        return await dbQuery
            .OrderBy(l => l.Section.Position)
            .ThenBy(l => l.OrderIndex)
            .Select(l => new LessonOrderInfo(
                l.Id, l.SectionId, l.Title, l.Type, l.OrderIndex))
            .ToListAsync(ct);
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

    private record LessonOrderInfo(int Id, int SectionId, string Title, LessonType Type, int OrderIndex);
}

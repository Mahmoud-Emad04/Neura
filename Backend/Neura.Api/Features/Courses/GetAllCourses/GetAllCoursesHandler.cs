using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions.Specification;
using Neura.Core.Enums;
using Neura.Core.Specifications.Courses;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.GetAllCourses;

internal sealed class GetAllCoursesHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers,
    HybridCache hybridCache)
    : IRequestHandler<GetAllCoursesQuery, Result<PaginatedList<CourseSummaryResponse>>>
{
    private static readonly HybridCacheEntryOptions StatsCacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(2),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    public async Task<Result<PaginatedList<CourseSummaryResponse>>> Handle(
        GetAllCoursesQuery request, CancellationToken ct)
    {
        var filters = request.Filters;
        var spec = new CourseFilterSpecification(filters);

        var query = SpecificationEvaluator.GetQuery(context.Courses.AsNoTracking().Where(c => c.Status != CourseStatus.Pending), spec);

        var projectedQuery = query.ProjectToType<CourseSummaryResponse>();

        var baseUrl = helpers.GetBaseUrl();

        var paginatedCourses = await PaginatedList<CourseSummaryResponse>.CreateAsync(
            projectedQuery,
            filters.PageNumber,
            filters.PageSize,
            c => c.ImageUrl = $"{baseUrl}/{c.ImageUrl}",
            ct
        );

        // ── Batch course stats (fix N+1) ─────────────────────────────
        var courseIds = paginatedCourses.Items
            .Select(c => TryDecodeCourseId(c.KeyId, out var id) ? id : 0)
            .Where(id => id != 0)
            .ToList();

        if (courseIds.Count > 0)
        {
            var cacheKey = CacheKeys.CourseStats(courseIds);

            var stats = await hybridCache.GetOrCreateAsync(
                cacheKey,
                async cancel =>
                {
                    // Fetch lesson data into memory to compute TotalHours (EF can't translate TimeSpan.TotalHours)
                    var lessonData = await context.Lessons
                        .AsNoTracking()
                        .Where(l => courseIds.Contains(l.Section.CourseId) && !l.IsDeleted)
                        .Select(l => new { l.Section.CourseId, l.Duration })
                        .ToListAsync(cancel);

                    var lessonStats = lessonData
                        .GroupBy(l => l.CourseId)
                        .Select(g => new
                        {
                            CourseId = g.Key,
                            LessonCount = g.Count(),
                            TotalHours = (int)g.Sum(l => l.Duration.TotalHours)
                        })
                        .ToDictionary(x => x.CourseId);

                    // Single query for student counts per course
                    var studentCounts = await context.CourseUsers
                        .AsNoTracking()
                        .Where(cu => courseIds.Contains(cu.CourseId) && !cu.IsDeleted)
                        .GroupBy(cu => cu.CourseId)
                        .Select(g => new { CourseId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancel);

                    return courseIds.ToDictionary(
                        id => id,
                        id => new CourseStatsEntry
                        {
                            NumberOfLessons = lessonStats.TryGetValue(id, out var ls) ? ls.LessonCount : 0,
                            Hours = lessonStats.TryGetValue(id, out var lh) ? lh.TotalHours : 0,
                            NumberOfStudents = studentCounts.GetValueOrDefault(id, 0)
                        });
                },
                StatsCacheOptions,
                cancellationToken: ct);

            foreach (var course in paginatedCourses.Items)
            {
                if (TryDecodeCourseId(course.KeyId, out var courseId) && stats.TryGetValue(courseId, out var s))
                {
                    course.NumberOfLessons = s.NumberOfLessons;
                    course.Hours = s.Hours;
                    course.NumberOfStudents = s.NumberOfStudents;
                }
            }
        }

        // ── User-specific data (bookmarks + enrollment) ──────────────
        if (request.UserId is not null)
        {
            var bookmarkedCourseIds = await context.CourseBookmarks
                .Where(b => b.UserId == request.UserId && !b.IsDeleted)
                .Select(b => b.CourseId)
                .ToListAsync(ct);

            var enrolledCourseIds = await context.CourseUsers
                .Where(cu => courseIds.Contains(cu.CourseId) && cu.UserId == request.UserId && !cu.IsDeleted)
                .Select(cu => cu.CourseId)
                .ToHashSetAsync(ct);

            foreach (var course in paginatedCourses.Items)
            {
                if (TryDecodeCourseId(course.KeyId, out var id))
                {
                    course.IsBookmarked = bookmarkedCourseIds.Contains(id);
                    course.IsEnrolled = enrolledCourseIds.Contains(id);
                }
            }
        }

        return Result.Success(paginatedCourses);
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

/// <summary>Serializable DTO for cached course statistics.</summary>
public sealed class CourseStatsEntry
{
    public int NumberOfLessons { get; init; }
    public int Hours { get; init; }
    public int NumberOfStudents { get; init; }
}


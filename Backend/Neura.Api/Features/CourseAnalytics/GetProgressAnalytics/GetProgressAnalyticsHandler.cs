using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.CourseAnalytics.GetProgressAnalytics;

internal sealed class GetProgressAnalyticsHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetProgressAnalyticsQuery, Result<ProgressAnalytics>>
{
    public async Task<Result<ProgressAnalytics>> Handle(
        GetProgressAnalyticsQuery query, CancellationToken ct)
    {
        // ── Resolve HashId → int ──────────────────────────────────────────────
        var numbers = helpers.DecodeHash(query.CourseKeyId);
        if (numbers.Length == 0)
            return Result.Failure<ProgressAnalytics>(AnalyticsErrors.CourseNotFound);

        var courseId = numbers[0];
        var courseExists = await context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId, ct);

        if (!courseExists)
            return Result.Failure<ProgressAnalytics>(AnalyticsErrors.CourseNotFound);

        // ── Date boundaries ───────────────────────────────────────────────────
        DateTime? fromUtc = query.From.HasValue
            ? query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : null;

        DateTime? toUtcExclusive = query.To.HasValue
            ? query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : null;

        // ── Total / published lesson counts (not date-filtered; structural data) ──
        var lessonCounts = await context.Lessons
            .AsNoTracking()
            .Where(l => l.Section.CourseId == courseId)
            .GroupBy(l => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Published = g.Count(l => l.IsPublished)
            })
            .FirstOrDefaultAsync(ct);

        var totalLessons = lessonCounts?.Total ?? 0;
        var publishedLessons = lessonCounts?.Published ?? 0;

        if (totalLessons == 0)
        {
            return Result.Success(new ProgressAnalytics
            {
                FilterFrom = query.From,
                FilterTo = query.To,
                TotalLessons = 0,
                PublishedLessons = 0
            });
        }

        // ── Per-student lesson completion counts ─────────────────────────────
        var completionsQuery = context.LessonCompletions
            .AsNoTracking()
            .Where(lc => lc.Lesson.Section.CourseId == courseId && !lc.IsDeleted);

        if (fromUtc.HasValue)
            completionsQuery = completionsQuery.Where(lc => lc.CompletedOn >= fromUtc.Value);
        if (toUtcExclusive.HasValue)
            completionsQuery = completionsQuery.Where(lc => lc.CompletedOn < toUtcExclusive.Value);

        var perStudentCompletions = await completionsQuery
            .GroupBy(lc => lc.UserId)
            .Select(g => new { UserId = g.Key, CompletedCount = g.Count() })
            .ToListAsync(ct);

        if (!perStudentCompletions.Any())
        {
            return Result.Success(new ProgressAnalytics
            {
                FilterFrom = query.From,
                FilterTo = query.To,
                TotalLessons = totalLessons,
                PublishedLessons = publishedLessons
            });
        }

        // ── Compute completion percentages ───────────────────────────────────
        var perStudentPct = perStudentCompletions
            .Select(s => (decimal)s.CompletedCount / totalLessons * 100m)
            .ToList();

        var avgCompletion = Math.Round(perStudentPct.Average(), 2);
        var completed100 = perStudentCompletions.Count(s => s.CompletedCount >= totalLessons);

        var buckets = new[]
        {
            ("0-25%",  perStudentPct.Count(p => p <= 25)),
            ("26-50%", perStudentPct.Count(p => p > 25 && p <= 50)),
            ("51-75%", perStudentPct.Count(p => p > 50 && p <= 75)),
            ("76-99%", perStudentPct.Count(p => p > 75 && p < 100)),
            ("100%",   perStudentPct.Count(p => p >= 100))
        };

        return Result.Success(new ProgressAnalytics
        {
            FilterFrom = query.From,
            FilterTo = query.To,
            TotalLessons = totalLessons,
            PublishedLessons = publishedLessons,
            AverageCompletionPercentage = avgCompletion,
            StudentsCompleted100Percent = completed100,
            CompletionDistribution = buckets
                .Select(b => new CompletionBucket { Range = b.Item1, Count = b.Item2 })
                .ToList()
        });
    }
}

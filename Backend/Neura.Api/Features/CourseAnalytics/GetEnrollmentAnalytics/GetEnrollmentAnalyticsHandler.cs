using MediatR;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.CourseAnalytics.GetEnrollmentAnalytics;

internal sealed class GetEnrollmentAnalyticsHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetEnrollmentAnalyticsQuery, Result<EnrollmentAnalytics>>
{
    public async Task<Result<EnrollmentAnalytics>> Handle(
        GetEnrollmentAnalyticsQuery query, CancellationToken ct)
    {
        // ── Resolve HashId → int ──────────────────────────────────────────────
        var numbers = helpers.DecodeHash(query.CourseKeyId);
        if (numbers.Length == 0)
            return Result.Failure<EnrollmentAnalytics>(AnalyticsErrors.CourseNotFound);

        var courseId = numbers[0];
        var courseExists = await context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId, ct);

        if (!courseExists)
            return Result.Failure<EnrollmentAnalytics>(AnalyticsErrors.CourseNotFound);

        // ── Date boundary helpers ─────────────────────────────────────────────
        DateTime? fromUtc = query.From.HasValue
            ? query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : null;

        DateTime? toUtcExclusive = query.To.HasValue
            ? query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : null;

        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var oneWeekAgo = now.AddDays(-7);
        var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // ── Base query: non-deleted students ─────────────────────────────────
        var studentLevel = (int)CourseRoleType.Student;

        var baseStudents = context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .Where(cu => cu.CourseId == courseId
                      && cu.CourseRole.Level == studentLevel
                      && !cu.IsDeleted);

        // Total enrolled (always all-time)
        var totalStudents = await baseStudents.CountAsync(ct);

        // Active: accessed within range (or last 30 days when no filter)
        var activeFrom = fromUtc ?? thirtyDaysAgo;
        var activeTo = toUtcExclusive ?? now;
        var activeStudents = await baseStudents
            .CountAsync(cu => cu.LastAccessedOn.HasValue
                           && cu.LastAccessedOn.Value >= activeFrom
                           && cu.LastAccessedOn.Value < activeTo, ct);

        // Convenience buckets — only when no custom range is applied
        int newThisWeek = 0, newThisMonth = 0;
        if (!fromUtc.HasValue && !toUtcExclusive.HasValue)
        {
            newThisWeek = await baseStudents
                .CountAsync(cu => cu.EnrolledOn >= oneWeekAgo, ct);

            newThisMonth = await baseStudents
                .CountAsync(cu => cu.EnrolledOn >= firstOfMonth, ct);
        }

        // Enrollment trend: daily buckets within the filter range (or last 30 days)
        var trendFrom = fromUtc ?? thirtyDaysAgo;
        var trendTo = toUtcExclusive ?? now;

        var enrolledDates = await baseStudents
            .Where(cu => cu.EnrolledOn >= trendFrom && cu.EnrolledOn < trendTo)
            .Select(cu => cu.EnrolledOn)
            .ToListAsync(ct);

        var trend = enrolledDates
            .GroupBy(d => DateOnly.FromDateTime(d))
            .Select(g => new DailyEnrollmentCount { Date = g.Key, Count = g.Count() })
            .OrderBy(d => d.Date)
            .ToList();

        return Result.Success(new EnrollmentAnalytics
        {
            FilterFrom = query.From,
            FilterTo = query.To,
            TotalStudents = totalStudents,
            ActiveStudents = activeStudents,
            NewThisWeek = newThisWeek,
            NewThisMonth = newThisMonth,
            EnrollmentTrend = trend
        });
    }
}

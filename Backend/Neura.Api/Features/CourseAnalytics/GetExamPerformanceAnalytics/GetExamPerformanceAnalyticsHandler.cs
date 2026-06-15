using MediatR;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.CourseAnalytics.GetExamPerformanceAnalytics;

internal sealed class GetExamPerformanceAnalyticsHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetExamPerformanceAnalyticsQuery, Result<ExamSummaryAnalytics>>
{
    public async Task<Result<ExamSummaryAnalytics>> Handle(
        GetExamPerformanceAnalyticsQuery query, CancellationToken ct)
    {
        // ── Resolve HashId → int ──────────────────────────────────────────────
        var numbers = helpers.DecodeHash(query.CourseKeyId);
        if (numbers.Length == 0)
            return Result.Failure<ExamSummaryAnalytics>(AnalyticsErrors.CourseNotFound);

        var courseId = numbers[0];
        var courseExists = await context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId, ct);

        if (!courseExists)
            return Result.Failure<ExamSummaryAnalytics>(AnalyticsErrors.CourseNotFound);

        // ── Date boundaries ───────────────────────────────────────────────────
        DateTime? fromUtc = query.From.HasValue
            ? query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : null;

        DateTime? toUtcExclusive = query.To.HasValue
            ? query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : null;

        // ── Load all exams belonging to this course ───────────────────────────
        var exams = await context.Exams
            .AsNoTracking()
            .Where(e => e.Lesson.Section.CourseId == courseId)
            .Select(e => new { e.Id, e.Title })
            .ToListAsync(ct);

        if (!exams.Any())
        {
            return Result.Success(new ExamSummaryAnalytics
            {
                FilterFrom = query.From,
                FilterTo = query.To,
                TotalExams = 0
            });
        }

        var examIds = exams.Select(e => e.Id).ToList();

        // ── Per-exam aggregation pushed to SQL ────────────────────────────────
        var attemptsQuery = context.ExamAttempts
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId)
                     && a.Status != AttemptStatus.InProgress);

        if (fromUtc.HasValue)
            attemptsQuery = attemptsQuery.Where(a => a.StartedAt >= fromUtc.Value);
        if (toUtcExclusive.HasValue)
            attemptsQuery = attemptsQuery.Where(a => a.StartedAt < toUtcExclusive.Value);

        var perExamStats = await attemptsQuery
            .GroupBy(a => a.ExamId)
            .Select(g => new
            {
                ExamId = g.Key,
                AttemptCount = g.Count(),
                AvgScore = g.Average(a => (decimal?)a.ScorePercentage) ?? 0m,
                PassedCount = g.Count(a => a.Passed == true)
            })
            .ToListAsync(ct);

        // ── Build per-exam mini summaries ─────────────────────────────────────
        var perExamSummaries = exams
            .Select(e =>
            {
                var stats = perExamStats.FirstOrDefault(s => s.ExamId == e.Id);
                var attemptCount = stats?.AttemptCount ?? 0;
                var passRate = attemptCount > 0
                    ? Math.Round((decimal)stats!.PassedCount / attemptCount * 100, 2)
                    : 0m;

                return new ExamMiniSummary
                {
                    ExamId = e.Id,
                    ExamTitle = e.Title,
                    AttemptCount = attemptCount,
                    AverageScorePercentage = stats is not null
                        ? Math.Round(stats.AvgScore, 2)
                        : 0m,
                    PassRate = passRate
                };
            })
            .ToList();

        // ── Overall aggregates across exams that have attempts ────────────────
        var withAttempts = perExamSummaries.Where(e => e.AttemptCount > 0).ToList();
        var overallAvg = withAttempts.Any()
            ? Math.Round(withAttempts.Average(e => e.AverageScorePercentage), 2)
            : 0m;
        var overallPassRate = withAttempts.Any()
            ? Math.Round(withAttempts.Average(e => e.PassRate), 2)
            : 0m;

        return Result.Success(new ExamSummaryAnalytics
        {
            FilterFrom = query.From,
            FilterTo = query.To,
            TotalExams = exams.Count,
            OverallAverageScore = overallAvg,
            OverallPassRate = overallPassRate,
            PerExamSummary = perExamSummaries
        });
    }
}

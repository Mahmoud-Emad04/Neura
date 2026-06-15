using MediatR;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAnalytics.GetScoreDistribution;

internal sealed class GetScoreDistributionHandler(ApplicationDbContext context)
    : IRequestHandler<GetScoreDistributionQuery, Result<ScoreDistributionResponse>>
{
    public async Task<Result<ScoreDistributionResponse>> Handle(
        GetScoreDistributionQuery query, CancellationToken ct)
    {
        var exam = await context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == query.ExamId, ct);

        if (exam is null)
            return Result.Failure<ScoreDistributionResponse>(AnalyticsErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await ExamAnalyticsHelpers.HasInstructorPermissionAsync(context, courseId, query.UserId))
            return Result.Failure<ScoreDistributionResponse>(AnalyticsErrors.Forbidden);

        var percentages = await context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == exam.Id
                     && a.Status != AttemptStatus.InProgress
                     && a.ScorePercentage.HasValue)
            .Select(a => a.ScorePercentage!.Value)
            .ToListAsync(ct);

        if (!percentages.Any())
            return Result.Failure<ScoreDistributionResponse>(AnalyticsErrors.NoAttempts);

        var totalCount = percentages.Count;
        var buckets = new List<ScoreBucket>();

        for (int i = 0; i < 10; i++)
        {
            var lower = i * 10;
            var upper = (i + 1) * 10;
            var label = i == 0 ? $"0-{upper}" : $"{lower + 1}-{upper}";

            var count = percentages.Count(p =>
                i == 0
                    ? p >= lower && p <= upper
                    : p > lower && p <= upper);

            buckets.Add(new ScoreBucket
            {
                Range = label,
                Count = count,
                Percentage = Math.Round((decimal)count / totalCount * 100, 2)
            });
        }

        return Result.Success(new ScoreDistributionResponse { Buckets = buckets });
    }
}

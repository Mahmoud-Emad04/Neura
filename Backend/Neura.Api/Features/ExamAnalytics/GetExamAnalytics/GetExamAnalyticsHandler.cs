using MediatR;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAnalytics.GetExamAnalytics;

internal sealed class GetExamAnalyticsHandler(ApplicationDbContext context)
    : IRequestHandler<GetExamAnalyticsQuery, Result<ExamAnalyticsResponse>>
{
    public async Task<Result<ExamAnalyticsResponse>> Handle(
        GetExamAnalyticsQuery query, CancellationToken ct)
    {
        var exam = await context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .Include(e => e.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.LessonId == query.ExamId, ct);

        if (exam is null)
            return Result.Failure<ExamAnalyticsResponse>(AnalyticsErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await ExamAnalyticsHelpers.HasInstructorPermissionAsync(context, courseId, query.UserId))
            return Result.Failure<ExamAnalyticsResponse>(AnalyticsErrors.Forbidden);

        var completedStatuses = new[]
        {
            AttemptStatus.Submitted, AttemptStatus.TimedOut,
            AttemptStatus.AutoSubmitted, AttemptStatus.Graded
        };

        var allAttempts = await context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == exam.Id)
            .ToListAsync(ct);

        var completedAttempts = allAttempts
            .Where(a => completedStatuses.Contains(a.Status))
            .ToList();

        var inProgressCount = allAttempts.Count(a => a.Status == AttemptStatus.InProgress);
        var totalAttempts = allAttempts.Count;
        var uniqueStudents = allAttempts.Select(a => a.UserId).Distinct().Count();

        decimal avgScore = 0, avgPercentage = 0, highest = 0, lowest = 0, median = 0;
        int passedCount = 0, failedCount = 0;

        if (completedAttempts.Any())
        {
            var percentages = completedAttempts
                .Where(a => a.ScorePercentage.HasValue)
                .Select(a => a.ScorePercentage!.Value)
                .OrderBy(p => p)
                .ToList();

            if (percentages.Any())
            {
                avgScore = Math.Round(completedAttempts.Average(a => a.Score ?? 0), 2);
                avgPercentage = Math.Round(percentages.Average(), 2);
                highest = percentages.Last();
                lowest = percentages.First();
                median = ExamAnalyticsHelpers.CalculateMedian(percentages);
            }

            passedCount = completedAttempts.Count(a => a.Passed == true);
            failedCount = completedAttempts.Count(a => a.Passed == false);
        }

        var passRate = completedAttempts.Any()
            ? Math.Round((decimal)passedCount / completedAttempts.Count * 100, 2)
            : 0;

        var violationStats = await context.AttemptViolations
            .AsNoTracking()
            .Where(v => v.ExamAttempt.ExamId == exam.Id)
            .GroupBy(v => v.ExamAttemptId)
            .Select(g => new { AttemptId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalViolations = violationStats.Sum(v => v.Count);
        var studentsWithViolations = violationStats.Count;

        var questionAnalytics = await ExamAnalyticsHelpers.BuildQuestionAnalyticsAsync(context, exam, exam.Id);

        return Result.Success(new ExamAnalyticsResponse
        {
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            TotalAttempts = totalAttempts,
            UniqueStudents = uniqueStudents,
            CompletedAttempts = completedAttempts.Count,
            InProgressAttempts = inProgressCount,
            TimedOutAttempts = completedAttempts.Count(a => a.Status == AttemptStatus.TimedOut),
            AutoSubmittedAttempts = completedAttempts.Count(a => a.Status == AttemptStatus.AutoSubmitted),
            AverageScore = avgScore,
            AverageScorePercentage = avgPercentage,
            HighestScorePercentage = highest,
            LowestScorePercentage = lowest,
            MedianScorePercentage = median,
            PassedCount = passedCount,
            FailedCount = failedCount,
            PassRate = passRate,
            TotalViolations = totalViolations,
            StudentsWithViolations = studentsWithViolations,
            Questions = questionAnalytics
        });
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAnalytics.GetStudentExamAnalytics;

internal sealed class GetStudentExamAnalyticsHandler(ApplicationDbContext context)
    : IRequestHandler<GetStudentExamAnalyticsQuery, Result<StudentExamAnalyticsResponse>>
{
    public async Task<Result<StudentExamAnalyticsResponse>> Handle(
        GetStudentExamAnalyticsQuery query, CancellationToken ct)
    {
        var exam = await context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == query.ExamId, ct);

        if (exam is null)
            return Result.Failure<StudentExamAnalyticsResponse>(AnalyticsErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        
        var isEnrolled = await context.CourseUsers
            .AnyAsync(cu => cu.CourseId == courseId && cu.UserId == query.UserId, ct);

        if (!isEnrolled)
            return Result.Failure<StudentExamAnalyticsResponse>(AnalyticsErrors.Forbidden);

        var completedStatuses = new[]
        {
            AttemptStatus.Submitted, AttemptStatus.TimedOut,
            AttemptStatus.AutoSubmitted, AttemptStatus.Graded
        };

        var allCompletedAttempts = await context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == exam.Id && completedStatuses.Contains(a.Status))
            .ToListAsync(ct);

        var classScores = allCompletedAttempts
            .Where(a => a.ScorePercentage.HasValue)
            .Select(a => a.ScorePercentage!.Value)
            .ToList();

        var studentAttempts = allCompletedAttempts
            .Where(a => a.UserId == query.UserId)
            .ToList();

        var studentScores = studentAttempts
            .Where(a => a.ScorePercentage.HasValue)
            .Select(a => a.ScorePercentage!.Value)
            .ToList();

        decimal? studentBestScore = studentScores.Any() ? studentScores.Max() : null;

        decimal classAvg = 0, classHigh = 0, classMedian = 0;
        decimal? studentPercentile = null;

        if (classScores.Any())
        {
            var sortedClassScores = classScores.OrderBy(s => s).ToList();
            classAvg = Math.Round(classScores.Average(), 2);
            classHigh = sortedClassScores.Last();
            classMedian = ExamAnalyticsHelpers.CalculateMedian(sortedClassScores);

            if (studentBestScore.HasValue)
            {
                var scoresBelow = sortedClassScores.Count(s => s < studentBestScore.Value);
                studentPercentile = Math.Round((decimal)scoresBelow / sortedClassScores.Count * 100, 2);
            }
        }

        return Result.Success(new StudentExamAnalyticsResponse
        {
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            StudentScorePercentage = studentBestScore,
            TotalStudentAttempts = studentAttempts.Count,
            HasCompletedAttempt = studentAttempts.Any(),
            ClassAveragePercentage = classAvg,
            ClassHighestPercentage = classHigh,
            ClassMedianPercentage = classMedian,
            TotalClassAttempts = allCompletedAttempts.Count,
            StudentPercentile = studentPercentile
        });
    }
}

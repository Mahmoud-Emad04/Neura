using MediatR;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using System.Text.Json;

namespace Neura.Api.Features.ExamAnalytics.GetStudentAttemptDetail;

internal sealed class GetStudentAttemptDetailHandler(ApplicationDbContext context)
    : IRequestHandler<GetStudentAttemptDetailQuery, Result<AttemptResultResponse>>
{
    public async Task<Result<AttemptResultResponse>> Handle(
        GetStudentAttemptDetailQuery query, CancellationToken ct)
    {
        var attempt = await context.ExamAttempts
            .AsNoTracking()
            .Include(a => a.Exam)
                .ThenInclude(e => e.Lesson)
                    .ThenInclude(l => l.Section)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedOptions)
            .Include(a => a.Violations)
            .FirstOrDefaultAsync(a => a.Id == query.AttemptId, ct);

        if (attempt is null)
            return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.AttemptNotFound);

        var courseId = attempt.Exam.Lesson.Section.CourseId;
        if (!await ExamAnalyticsHelpers.HasInstructorPermissionAsync(context, courseId, query.UserId))
            return Result.Failure<AttemptResultResponse>(AnalyticsErrors.Forbidden);

        if (attempt.Status == AttemptStatus.InProgress)
            return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.ResultsNotAvailable);

        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();

        var ar = await ExamAnalyticsHelpers.BuildAttemptQuestionResultsAsync(
            context, attempt, questionOrder);

        var totalPoints = ar.Questions.Sum(q => q.Points);

        return Result.Success(new AttemptResultResponse
        {
            AttemptId = attempt.Id,
            Score = attempt.Score ?? 0,
            ScorePercentage = attempt.ScorePercentage ?? 0,
            TotalPoints = totalPoints,
            PassingScorePercentage = attempt.Exam.PassingScorePercentage,
            Passed = attempt.Passed ?? false,
            Status = attempt.Status.ToString(),
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt ?? attempt.StartedAt,
            TotalQuestions = questionOrder.Count,
            CorrectAnswers = ar.CorrectCount,
            WrongAnswers = ar.WrongCount,
            Unanswered = ar.Unanswered,
            ViolationCount = attempt.Violations.Count,
            Questions = ar.Questions
        });
    }
}

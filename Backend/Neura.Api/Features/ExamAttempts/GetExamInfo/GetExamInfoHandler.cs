using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAttempts.GetExamInfo;

internal sealed class GetExamInfoHandler(ApplicationDbContext context) 
    : IRequestHandler<GetExamInfoQuery, Result<ExamInfoResponse>>
{
    public async Task<Result<ExamInfoResponse>> Handle(
        GetExamInfoQuery query, CancellationToken ct)
    {
        var lessonId = query.LessonId;
        var userId = query.UserId;

        var exam = await context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId, ct);

        if (exam is null)
            return Result.Failure<ExamInfoResponse>(ExamAttemptErrors.ExamNotFound);

        if (!exam.IsPublished)
            return Result.Failure<ExamInfoResponse>(ExamAttemptErrors.ExamNotPublished);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await ExamAttemptHelpers.IsEnrolledStudentAsync(context, courseId, userId))
            return Result.Failure<ExamInfoResponse>(ExamAttemptErrors.NotEnrolled);

        var attemptsTaken = await context.ExamAttempts
            .AsNoTracking()
            .CountAsync(a => a.ExamId == exam.Id && a.UserId == userId, ct);

        var inProgressAttempt = await context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == exam.Id
                     && a.UserId == userId
                     && a.Status == AttemptStatus.InProgress)
            .Select(a => new { a.Id, a.StartedAt })
            .FirstOrDefaultAsync(ct);

        bool hasInProgressAttempt = false;
        int? inProgressAttemptId = null;

        if (inProgressAttempt is not null)
        {
            if (ExamAttemptHelpers.IsTimedOut(inProgressAttempt.StartedAt, exam.DurationInMinutes))
            {
                hasInProgressAttempt = false;
            }
            else
            {
                hasInProgressAttempt = true;
                inProgressAttemptId = inProgressAttempt.Id;
            }
        }

        var questionCount = exam.NumberOfQuestionsToServe ?? exam.Questions.Count;

        var totalPoints = exam.NumberOfQuestionsToServe.HasValue
            ? exam.Questions.OrderBy(q => q.Points).Take(questionCount).Sum(q => q.Points)
            : exam.Questions.Sum(q => q.Points);

        int? remainingAttempts = exam.MaxAttempts.HasValue
            ? Math.Max(0, exam.MaxAttempts.Value - attemptsTaken)
            : null;

        var response = new ExamInfoResponse
        {
            ExamId = lessonId,
            Title = exam.Title,
            Description = exam.Description,
            DurationInMinutes = exam.DurationInMinutes,
            QuestionCount = questionCount,
            TotalPoints = totalPoints,
            PassingScorePercentage = exam.PassingScorePercentage,
            MaxAttempts = exam.MaxAttempts,
            AttemptsTaken = attemptsTaken,
            RemainingAttempts = remainingAttempts,
            EnableTabSwitchDetection = exam.EnableTabSwitchDetection,
            MaxViolationsBeforeAutoSubmit = exam.MaxViolationsBeforeAutoSubmit,
            HasInProgressAttempt = hasInProgressAttempt,
            InProgressAttemptId = inProgressAttemptId
        };

        return Result.Success(response);
    }
}

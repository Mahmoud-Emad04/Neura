using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Api.Features.Lessons.MarkQuizLessonCompleted;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.Services;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAttempts.SubmitAttempt;

internal sealed class SubmitAttemptHandler(
    ApplicationDbContext context,
    IGradingService gradingService,
    ISender sender) 
    : IRequestHandler<SubmitAttemptCommand, Result<SubmitAttemptResponse>>
{
    public async Task<Result<SubmitAttemptResponse>> Handle(
        SubmitAttemptCommand command, CancellationToken ct)
    {
        var attemptId = command.AttemptId;
        var userId = command.UserId;

        var attempt = await context.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt is null)
            return Result.Failure<SubmitAttemptResponse>(ExamAttemptErrors.AttemptNotFound);

        if (attempt.UserId != userId)
            return Result.Failure<SubmitAttemptResponse>(ExamAttemptErrors.NotAttemptOwner);

        if (attempt.Status != AttemptStatus.InProgress)
            return Result.Failure<SubmitAttemptResponse>(ExamAttemptErrors.AttemptNotInProgress);

        var status = ExamAttemptHelpers.IsTimedOut(attempt.StartedAt, attempt.Exam.DurationInMinutes)
            ? AttemptStatus.TimedOut
            : AttemptStatus.Submitted;

        await gradingService.GradeAttemptAsync(attempt, status);

        var response = new SubmitAttemptResponse
        {
            AttemptId = attempt.Id,
            Score = attempt.Score!.Value,
            ScorePercentage = attempt.ScorePercentage!.Value,
            TotalPoints = await ExamAttemptHelpers.GetAttemptTotalPointsAsync(context, attempt),
            PassingScorePercentage = attempt.Exam.PassingScorePercentage,
            Passed = attempt.Passed!.Value,
            Status = attempt.Status.ToString(),
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt!.Value
        };

        if (attempt.Passed!.Value)
        {
            await sender.Send(new MarkQuizLessonCompletedCommand(attempt.Exam.LessonId, userId), ct);
        }

        return Result.Success(response);
    }
}

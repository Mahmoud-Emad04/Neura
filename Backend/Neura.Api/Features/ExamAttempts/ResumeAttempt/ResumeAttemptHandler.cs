using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.Services;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAttempts.ResumeAttempt;

internal sealed class ResumeAttemptHandler(
    ApplicationDbContext context,
    IGradingService gradingService) 
    : IRequestHandler<ResumeAttemptCommand, Result<StartAttemptResponse>>
{
    public async Task<Result<StartAttemptResponse>> Handle(
        ResumeAttemptCommand command, CancellationToken ct)
    {
        var attemptId = command.AttemptId;
        var userId = command.UserId;

        var attempt = await context.ExamAttempts
            .Include(a => a.Exam)
                .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.AnswerOptions)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt is null)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.AttemptNotFound);

        if (attempt.UserId != userId)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.NotAttemptOwner);

        if (attempt.Status != AttemptStatus.InProgress)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.AttemptNotInProgress);

        if (ExamAttemptHelpers.IsTimedOut(attempt.StartedAt, attempt.Exam.DurationInMinutes))
        {
            await gradingService.GradeAttemptAsync(attempt, AttemptStatus.TimedOut);
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.AttemptTimedOut);
        }

        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();
        var answerOrder = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(attempt.AnswerOrder) ?? new();

        var allQuestions = attempt.Exam.Questions.ToDictionary(q => q.Id);
        var servedQuestions = questionOrder
            .Where(id => allQuestions.ContainsKey(id))
            .Select(id => allQuestions[id])
            .ToList();

        var savedAnswers = attempt.AttemptAnswers
            .ToDictionary(
                aa => aa.QuestionId,
                aa => aa.SelectedOptions.Select(so => so.AnswerOptionId).ToList()
            );

        var response = ExamAttemptHelpers.BuildStartAttemptResponse(attempt, attempt.Exam, servedQuestions, answerOrder, savedAnswers);

        return Result.Success(response);
    }
}

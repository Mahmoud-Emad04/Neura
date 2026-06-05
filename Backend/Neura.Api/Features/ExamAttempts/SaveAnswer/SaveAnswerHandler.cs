using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.Services;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAttempts.SaveAnswer;

internal sealed class SaveAnswerHandler(
    ApplicationDbContext context,
    IGradingService gradingService) 
    : IRequestHandler<SaveAnswerCommand, Result>
{
    public async Task<Result> Handle(
        SaveAnswerCommand command, CancellationToken ct)
    {
        var attemptId = command.AttemptId;
        var questionId = command.QuestionId;
        var request = command.Request;
        var userId = command.UserId;

        var attempt = await context.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.AttemptAnswers.Where(aa => aa.QuestionId == questionId))
                .ThenInclude(aa => aa.SelectedOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt is null)
            return Result.Failure(ExamAttemptErrors.AttemptNotFound);

        if (attempt.UserId != userId)
            return Result.Failure(ExamAttemptErrors.NotAttemptOwner);

        if (attempt.Status != AttemptStatus.InProgress)
            return Result.Failure(ExamAttemptErrors.AttemptNotInProgress);

        if (ExamAttemptHelpers.IsTimedOut(attempt.StartedAt, attempt.Exam.DurationInMinutes))
        {
            await gradingService.GradeAttemptAsync(attempt, AttemptStatus.TimedOut);
            return Result.Failure(ExamAttemptErrors.AttemptTimedOut);
        }

        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();
        if (!questionOrder.Contains(questionId))
            return Result.Failure(ExamAttemptErrors.QuestionNotInAttempt);

        if (request.SelectedOptionIds.Any())
        {
            var validOptionIds = await context.AnswerOptions
                .AsNoTracking()
                .Where(ao => ao.QuestionId == questionId)
                .Select(ao => ao.Id)
                .ToHashSetAsync(ct);

            var invalidOptions = request.SelectedOptionIds
                .Where(id => !validOptionIds.Contains(id))
                .ToList();

            if (invalidOptions.Any())
                return Result.Failure(ExamAttemptErrors.InvalidSelectedOptions);
        }

        var question = await context.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is not null
            && question.QuestionType is QuestionType.SingleChoice or QuestionType.TrueFalse
            && request.SelectedOptionIds.Count > 1)
        {
            return Result.Failure(ExamAttemptErrors.SingleChoiceMultipleSelections);
        }

        var existingAnswer = attempt.AttemptAnswers.FirstOrDefault();

        if (existingAnswer is not null)
        {
            context.AttemptAnswerOptions.RemoveRange(existingAnswer.SelectedOptions);

            if (request.SelectedOptionIds.Any())
            {
                foreach (var optionId in request.SelectedOptionIds)
                {
                    existingAnswer.SelectedOptions.Add(new AttemptAnswerOption
                    {
                        AttemptAnswerId = existingAnswer.Id,
                        AnswerOptionId = optionId
                    });
                }
            }
            else
            {
                context.AttemptAnswers.Remove(existingAnswer);
            }
        }
        else if (request.SelectedOptionIds.Any())
        {
            var newAnswer = new AttemptAnswer
            {
                ExamAttemptId = attemptId,
                QuestionId = questionId,
                SelectedOptions = request.SelectedOptionIds
                    .Select(optionId => new AttemptAnswerOption
                    {
                        AnswerOptionId = optionId
                    }).ToList()
            };

            context.AttemptAnswers.Add(newAnswer);
        }

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

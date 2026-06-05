using Ganss.Xss;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Question;
using Neura.Core.Entities;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamQuestions.UpdateQuestion;

internal sealed class UpdateQuestionHandler(
    ApplicationDbContext context,
    HtmlSanitizer sanitizer) 
    : IRequestHandler<UpdateQuestionCommand, Result<QuestionResponse>>
{
    public async Task<Result<QuestionResponse>> Handle(
        UpdateQuestionCommand command, CancellationToken ct)
    {
        var questionId = command.QuestionId;
        var request = command.Request;
        var userId = command.UserId;

        var question = await context.Questions
            .Include(q => q.AnswerOptions)
            .Include(q => q.Exam)
                .ThenInclude(e => e.Lesson)
                    .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            return Result.Failure<QuestionResponse>(QuestionErrors.QuestionNotFound);

        var courseId = question.Exam.Lesson.Section.CourseId;
        var courseUser = await context.CourseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId, ct);

        if (courseUser is null || (courseUser.PermissionMask & CoursePermissionMasks.CoInstructor) != CoursePermissionMasks.CoInstructor)
            return Result.Failure<QuestionResponse>(QuestionErrors.Forbidden);

        var hasAttempts = await context.AttemptAnswers
            .AnyAsync(aa => aa.QuestionId == questionId, ct);

        if (hasAttempts)
        {
            var restrictedResult = await ApplyRestrictedUpdateAsync(question, request);
            if (restrictedResult.IsFailure)
                return Result.Failure<QuestionResponse>(restrictedResult.Error);
        }
        else
        {
            ApplyFullUpdate(question, request);
        }

        question.Exam.UpdatedOn = DateTime.UtcNow;
        question.Exam.UpdatedById = userId;
        await context.SaveChangesAsync(ct);

        var response = question.Adapt<QuestionResponse>();
        response.HasAttempts = hasAttempts;

        return Result.Success(response);
    }

    private async Task<Result> ApplyRestrictedUpdateAsync(
        Question question, UpdateQuestionRequest request)
    {
        if (request.QuestionType != question.QuestionType)
            return Result.Failure(QuestionErrors.CannotChangeQuestionType);

        var existingCorrectIds = question.AnswerOptions
            .Where(a => a.IsCorrect)
            .Select(a => a.Id)
            .ToHashSet();

        var requestCorrectIds = request.Options
            .Where(o => o.Id.HasValue && o.IsCorrect)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        if (!existingCorrectIds.SetEquals(requestCorrectIds))
            return Result.Failure(QuestionErrors.CannotChangeCorrectAnswers);

        var existingOptionIds = question.AnswerOptions.Select(a => a.Id).ToHashSet();
        var requestOptionIds = request.Options
            .Where(o => o.Id.HasValue)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        var removedOptionIds = existingOptionIds.Except(requestOptionIds).ToList();

        if (removedOptionIds.Any())
        {
            var removedOptionHasSelections = await context.AttemptAnswerOptions
                .AnyAsync(aao => removedOptionIds.Contains(aao.AnswerOptionId));

            if (removedOptionHasSelections)
                return Result.Failure(QuestionErrors.CannotRemoveSelectedOptions);
        }

        if (request.Options.Any(o => !o.Id.HasValue))
            return Result.Failure(QuestionErrors.CannotAddOptionsAfterAttempts);

        question.QuestionText = sanitizer.Sanitize(request.QuestionText);
        question.Points = request.Points;

        foreach (var optionRequest in request.Options.Where(o => o.Id.HasValue))
        {
            var existing = question.AnswerOptions
                .FirstOrDefault(a => a.Id == optionRequest.Id!.Value);

            if (existing is not null)
                existing.Text = sanitizer.Sanitize(optionRequest.Text);
        }

        return Result.Success();
    }

    private void ApplyFullUpdate(Question question, UpdateQuestionRequest request)
    {
        question.QuestionText = sanitizer.Sanitize(request.QuestionText);
        question.QuestionType = request.QuestionType;
        question.Points = request.Points;

        var requestOptionIds = request.Options
            .Where(o => o.Id.HasValue)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        var optionsToRemove = question.AnswerOptions
            .Where(a => !requestOptionIds.Contains(a.Id))
            .ToList();

        context.AnswerOptions.RemoveRange(optionsToRemove);

        var order = 1;
        foreach (var optionRequest in request.Options)
        {
            if (optionRequest.Id.HasValue)
            {
                var existing = question.AnswerOptions
                    .FirstOrDefault(a => a.Id == optionRequest.Id.Value);

                if (existing is not null)
                {
                    existing.Text = sanitizer.Sanitize(optionRequest.Text);
                    existing.IsCorrect = optionRequest.IsCorrect;
                    existing.Order = order;
                }
            }
            else
            {
                question.AnswerOptions.Add(new AnswerOption
                {
                    Text = sanitizer.Sanitize(optionRequest.Text),
                    IsCorrect = optionRequest.IsCorrect,
                    Order = order
                });
            }

            order++;
        }
    }
}

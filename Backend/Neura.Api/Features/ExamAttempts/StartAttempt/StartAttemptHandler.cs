using MediatR;
using Microsoft.AspNetCore.Identity;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using System.Text.Json;

namespace Neura.Api.Features.ExamAttempts.StartAttempt;

internal sealed class StartAttemptHandler(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    : IRequestHandler<StartAttemptCommand, Result<StartAttemptResponse>>
{
    public async Task<Result<StartAttemptResponse>> Handle(
        StartAttemptCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var userId = command.UserId;

        var exam = await context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .Include(e => e.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId, ct);

        if (exam is null)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.ExamNotFound);

        if (!exam.IsPublished)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.ExamNotPublished);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await ExamAttemptHelpers.IsEnrolledStudentAsync(context, userManager, courseId, userId))
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.NotEnrolled);

        var existingInProgress = await context.ExamAttempts
            .AnyAsync(a => a.ExamId == exam.Id
                        && a.UserId == userId
                        && a.Status == AttemptStatus.InProgress, ct);

        if (existingInProgress)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.AttemptAlreadyInProgress);

        if (exam.MaxAttempts.HasValue)
        {
            var attemptsTaken = await context.ExamAttempts
                .AsNoTracking()
                .CountAsync(a => a.ExamId == exam.Id && a.UserId == userId, ct);

            if (attemptsTaken >= exam.MaxAttempts.Value)
                return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.MaxAttemptsReached);
        }

        var allQuestions = exam.Questions.ToList();

        List<Question> servedQuestions;
        if (exam.NumberOfQuestionsToServe.HasValue
            && exam.NumberOfQuestionsToServe.Value < allQuestions.Count)
        {
            servedQuestions = allQuestions
                .OrderBy(_ => Guid.NewGuid())
                .Take(exam.NumberOfQuestionsToServe.Value)
                .ToList();
        }
        else
        {
            servedQuestions = allQuestions;
        }

        if (exam.ShuffleQuestions)
            servedQuestions = servedQuestions.OrderBy(_ => Guid.NewGuid()).ToList();
        else
            servedQuestions = servedQuestions.OrderBy(q => q.Order).ToList();

        var answerOrder = new Dictionary<int, List<int>>();
        foreach (var question in servedQuestions)
        {
            var optionIds = exam.ShuffleAnswers
                ? question.AnswerOptions.OrderBy(_ => Guid.NewGuid()).Select(a => a.Id).ToList()
                : question.AnswerOptions.OrderBy(a => a.Order).Select(a => a.Id).ToList();

            answerOrder[question.Id] = optionIds;
        }

        var questionOrderJson = JsonSerializer.Serialize(servedQuestions.Select(q => q.Id).ToList());
        var answerOrderJson = JsonSerializer.Serialize(answerOrder);

        var attempt = new ExamAttempt
        {
            ExamId = exam.Id,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            Status = AttemptStatus.InProgress,
            QuestionOrder = questionOrderJson,
            AnswerOrder = answerOrderJson
        };

        context.ExamAttempts.Add(attempt);
        await context.SaveChangesAsync(ct);

        var response = ExamAttemptHelpers.BuildStartAttemptResponse(attempt, exam, servedQuestions, answerOrder, null);

        return Result.Success(response);
    }
}

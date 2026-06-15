using MediatR;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Exams.PublishExam;

internal sealed class PublishExamHandler(ApplicationDbContext context)
    : IRequestHandler<PublishExamCommand, Result>
{
    public async Task<Result> Handle(
        PublishExamCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var userId = command.UserId;

        var exam = await context.Exams
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .Include(e => e.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId, ct);

        if (exam is null)
            return Result.Failure(ExamErrors.ExamNotFound);

        if (exam.IsPublished)
        {
            if (!exam.Questions.Any())
                return Result.Failure(ExamErrors.NoQuestions);

            var hasInvalidQuestions = exam.Questions
                .Any(q => !q.AnswerOptions.Any(a => a.IsCorrect));

            if (hasInvalidQuestions)
                return Result.Failure(ExamErrors.QuestionsWithoutCorrectAnswer);

            if (exam.NumberOfQuestionsToServe.HasValue
                && exam.NumberOfQuestionsToServe.Value > exam.Questions.Count)
                return Result.Failure(ExamErrors.PoolSizeExceedsTotalQuestions);

            exam.IsPublished = true;
            exam.UpdatedById = userId;
            exam.UpdatedOn = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }
        else
        {
            if (!exam.IsPublished)
                return Result.Failure(ExamErrors.AlreadyUnpublished);

            var hasAttempts = await context.ExamAttempts
                .AnyAsync(a => a.ExamId == exam.Id, ct);

            if (hasAttempts)
                return Result.Failure(ExamErrors.CannotUnpublishWithAttempts);

            exam.IsPublished = false;
            exam.UpdatedById = userId;
            exam.UpdatedOn = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}

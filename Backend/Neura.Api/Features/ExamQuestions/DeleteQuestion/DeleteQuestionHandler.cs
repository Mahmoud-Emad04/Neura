using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamQuestions.DeleteQuestion;

internal sealed class DeleteQuestionHandler(ApplicationDbContext context) 
    : IRequestHandler<DeleteQuestionCommand, Result>
{
    public async Task<Result> Handle(
        DeleteQuestionCommand command, CancellationToken ct)
    {
        var questionId = command.QuestionId;
        var userId = command.UserId;

        var question = await context.Questions
            .Include(q => q.Exam)
                .ThenInclude(e => e.Lesson)
                    .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(q => q.Id == questionId && !q.IsDeleted, ct);

        if (question is null)
            return Result.Failure(QuestionErrors.QuestionNotFound);

        var courseId = question.Exam.Lesson.Section.CourseId;
        var courseUser = await context.CourseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId, ct);

        if (courseUser is null || (courseUser.PermissionMask & CoursePermissionMasks.CoInstructor) != CoursePermissionMasks.CoInstructor)
            return Result.Failure(QuestionErrors.Forbidden);

        var hasAttempts = await context.AttemptAnswers
            .AnyAsync(aa => aa.QuestionId == questionId, ct);

        if (hasAttempts)
            return Result.Failure(QuestionErrors.CannotDeleteWithAttempts);

        question.IsDeleted = true;

        var remainingQuestions = await context.Questions
            .Where(q => q.ExamId == question.ExamId && q.Id != questionId)
            .OrderBy(q => q.Order)
            .ToListAsync(ct);

        for (int i = 0; i < remainingQuestions.Count; i++)
            remainingQuestions[i].Order = i + 1;

        question.Exam.UpdatedOn = DateTime.UtcNow;
        question.Exam.UpdatedById = userId;
        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

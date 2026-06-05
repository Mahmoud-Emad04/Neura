using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamQuestions.ReorderQuestions;

internal sealed class ReorderQuestionsHandler(ApplicationDbContext context) 
    : IRequestHandler<ReorderQuestionsCommand, Result>
{
    public async Task<Result> Handle(
        ReorderQuestionsCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var request = command.Request;
        var userId = command.UserId;

        var exam = await context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId, ct);

        if (exam is null)
            return Result.Failure(ExamErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        var courseUser = await context.CourseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId, ct);

        if (courseUser is null || (courseUser.PermissionMask & CoursePermissionMasks.CoInstructor) != CoursePermissionMasks.CoInstructor)
            return Result.Failure(QuestionErrors.Forbidden);

        var questions = await context.Questions
            .Where(q => q.ExamId == exam.Id)
            .ToListAsync(ct);

        var existingIds = questions.Select(q => q.Id).ToHashSet();
        var requestIds = request.OrderedQuestionIds.ToHashSet();

        if (!existingIds.SetEquals(requestIds))
            return Result.Failure(QuestionErrors.ReorderIdsMismatch);

        if (request.OrderedQuestionIds.Count != requestIds.Count)
            return Result.Failure(QuestionErrors.ReorderDuplicateIds);

        var questionLookup = questions.ToDictionary(q => q.Id);
        for (int i = 0; i < request.OrderedQuestionIds.Count; i++)
            questionLookup[request.OrderedQuestionIds[i]].Order = i + 1;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

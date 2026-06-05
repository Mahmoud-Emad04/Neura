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

namespace Neura.Api.Features.ExamQuestions.AddQuestion;

internal sealed class AddQuestionHandler(
    ApplicationDbContext context,
    HtmlSanitizer sanitizer) 
    : IRequestHandler<AddQuestionCommand, Result<QuestionResponse>>
{
    public async Task<Result<QuestionResponse>> Handle(
        AddQuestionCommand command, CancellationToken ct)
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
            return Result.Failure<QuestionResponse>(ExamErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        var courseUser = await context.CourseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId, ct);

        if (courseUser is null || (courseUser.PermissionMask & CoursePermissionMasks.CoInstructor) != CoursePermissionMasks.CoInstructor)
            return Result.Failure<QuestionResponse>(QuestionErrors.Forbidden);

        var maxOrder = await context.Questions
            .AsNoTracking()
            .Where(q => q.ExamId == exam.Id)
            .MaxAsync(q => (int?)q.Order, ct) ?? 0;

        var question = new Question
        {
            ExamId = exam.Id,
            QuestionText = sanitizer.Sanitize(request.QuestionText),
            QuestionType = request.QuestionType,
            Points = request.Points,
            Order = maxOrder + 1,
            AnswerOptions = request.Options.Select((opt, index) => new AnswerOption
            {
                Text = sanitizer.Sanitize(opt.Text),
                IsCorrect = opt.IsCorrect,
                Order = index + 1
            }).ToList()
        };

        context.Questions.Add(question);
        await context.SaveChangesAsync(ct);

        var response = question.Adapt<QuestionResponse>();
        response.HasAttempts = false;

        return Result.Success(response);
    }
}

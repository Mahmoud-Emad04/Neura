using Ganss.Xss;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Exam;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Exams.CreateExam;

internal sealed class CreateExamHandler(
    ApplicationDbContext context,
    HtmlSanitizer sanitizer) 
    : IRequestHandler<CreateExamCommand, Result<ExamResponse>>
{
    public async Task<Result<ExamResponse>> Handle(
        CreateExamCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var userId = command.UserId;

        var lesson = await context.Lessons
            .AsNoTracking()
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);

        if (lesson is null)
            return Result.Failure<ExamResponse>(ExamErrors.LessonNotFound);

        var courseUser = await context.CourseUsers
            .Where(cu => cu.UserId == userId && cu.CourseId == lesson.Section.CourseId)
            .FirstOrDefaultAsync(ct);

        if (courseUser is null || !CoursePermissionMasks.HasPermission(courseUser.PermissionMask, CoursePermission.EditContent))
            return Result.Failure<ExamResponse>(ExamErrors.Forbidden);

        if (lesson.Type != LessonType.Quiz)
            return Result.Failure<ExamResponse>(ExamErrors.LessonNotQuizType);

        var examExists = await context.Exams
            .AnyAsync(e => e.LessonId == request.LessonId, ct);

        if (examExists)
            return Result.Failure<ExamResponse>(ExamErrors.ExamAlreadyExists);

        var exam = request.Adapt<Exam>();
        exam.Title = sanitizer.Sanitize(request.Title);
        exam.Description = request.Description is not null
            ? sanitizer.Sanitize(request.Description)
            : null;
        exam.ShowCorrectAnswersAfterSubmit = true;
        exam.IsPublished = true;
        exam.CreatedById = userId;
        exam.CreatedOn = DateTime.UtcNow;

        context.Exams.Add(exam);

        await context.Lessons
            .Where(l => l.Id == request.LessonId)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(l => l.Status, LessonStatus.Active)
                .SetProperty(l => l.Duration, TimeSpan.FromMinutes(request.DurationInMinutes ?? 0)), ct);

        await context.SaveChangesAsync(ct);

        var response = exam.Adapt<ExamResponse>();
        return Result.Success(response);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Exams.DeleteExam;

internal sealed class DeleteExamHandler(ApplicationDbContext context) 
    : IRequestHandler<DeleteExamCommand, Result>
{
    public async Task<Result> Handle(
        DeleteExamCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var userId = command.UserId;

        var exam = await context.Exams
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId, ct);

        if (exam is null)
            return Result.Failure(ExamErrors.ExamNotFound);

        var hasAttempts = await context.ExamAttempts
            .AnyAsync(a => a.ExamId == exam.Id, ct);

        if (hasAttempts)
            return Result.Failure(ExamErrors.CannotDeleteWithAttempts);

        exam.IsDeleted = true;

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Exams.UnpublishExam;

internal sealed class UnpublishExamHandler(ApplicationDbContext context) 
    : IRequestHandler<UnpublishExamCommand, Result>
{
    public async Task<Result> Handle(
        UnpublishExamCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var userId = command.UserId;

        var exam = await context.Exams
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId, ct);

        if (exam is null)
            return Result.Failure(ExamErrors.ExamNotFound);

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

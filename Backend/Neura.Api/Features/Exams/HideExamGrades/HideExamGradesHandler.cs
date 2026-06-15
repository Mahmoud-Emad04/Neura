using MediatR;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Exams.HideExamGrades;

internal sealed class HideExamGradesHandler(ApplicationDbContext context)
    : IRequestHandler<HideExamGradesCommand, Result>
{
    public async Task<Result> Handle(
        HideExamGradesCommand command, CancellationToken ct)
    {
        var exam = await context.Exams
            .FirstOrDefaultAsync(e => e.Id == command.ExamId, ct);

        if (exam is null)
            return Result.Failure(ExamErrors.ExamNotFound);

        if (!exam.AreGradesPublished)
            return Result.Failure(ExamErrors.GradesAlreadyHidden);

        exam.HideGrades();
        exam.UpdatedById = command.UserId;
        exam.UpdatedOn = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

using MediatR;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAttempts.FlagAttemptViolation;

internal sealed class FlagAttemptViolationHandler(ApplicationDbContext context)
    : IRequestHandler<FlagAttemptViolationCommand, Result>
{
    public async Task<Result> Handle(
        FlagAttemptViolationCommand command, CancellationToken ct)
    {
        var attempt = await context.ExamAttempts
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct);

        if (attempt is null)
            return Result.Failure(ExamAttemptErrors.AttemptNotFound);

        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result.Failure(ExamAttemptErrors.ViolationReasonRequired);

        if (attempt.Status != AttemptStatus.Graded)
            return Result.Failure(ExamAttemptErrors.AttemptNotGraded);

        attempt.FlagForViolation(command.Reason);

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

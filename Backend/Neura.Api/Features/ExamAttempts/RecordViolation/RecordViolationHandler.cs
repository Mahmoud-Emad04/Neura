using MediatR;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAttempts.RecordViolation;

internal sealed class RecordViolationHandler(ApplicationDbContext context)
    : IRequestHandler<RecordViolationCommand, Result<ViolationResponse>>
{
    public async Task<Result<ViolationResponse>> Handle(
        RecordViolationCommand command, CancellationToken ct)
    {
        var attemptId = command.AttemptId;
        var request = command.Request;
        var userId = command.UserId;

        var attempt = await context.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Violations)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt is null)
            return Result.Failure<ViolationResponse>(ExamAttemptErrors.AttemptNotFound);

        if (attempt.UserId != userId)
            return Result.Failure<ViolationResponse>(ExamAttemptErrors.NotAttemptOwner);

        if (attempt.Status != AttemptStatus.InProgress)
            return Result.Failure<ViolationResponse>(ExamAttemptErrors.AttemptNotInProgress);

        var violation = new AttemptViolation
        {
            ExamAttemptId = attemptId,
            ViolationType = request.ViolationType,
            OccurredAt = request.OccurredAt
        };

        context.AttemptViolations.Add(violation);

        var totalViolations = attempt.Violations.Count + 1;
        var isCheating = attempt.Exam.EnableTabSwitchDetection
            && attempt.Exam.MaxViolationsBeforeAutoSubmit.HasValue
            && totalViolations >= attempt.Exam.MaxViolationsBeforeAutoSubmit.Value;

        await context.SaveChangesAsync(ct);

        var response = new ViolationResponse
        {
            TotalViolations = totalViolations,
            MaxViolationsBeforeAutoSubmit = attempt.Exam.MaxViolationsBeforeAutoSubmit,
            AttemptAutoSubmitted = false,
            IsCheating = isCheating
        };

        return Result.Success(response);
    }
}

using MediatR;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using System.Text.Json;

namespace Neura.Api.Features.ExamAttempts.ResolveViolation;

internal sealed class ResolveViolationHandler(ApplicationDbContext context)
    : IRequestHandler<ResolveViolationCommand, Result>
{
    public async Task<Result> Handle(
        ResolveViolationCommand command, CancellationToken ct)
    {
        var attempt = await context.ExamAttempts
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct);

        if (attempt is null)
            return Result.Failure(ExamAttemptErrors.AttemptNotFound);

        if (string.IsNullOrWhiteSpace(command.Notes))
            return Result.Failure(ExamAttemptErrors.InstructorNotesRequired);

        if (attempt.Status != AttemptStatus.ViolationFlagged)
            return Result.Failure(ExamAttemptErrors.AttemptNotFlagged);

        // Compute total points from the attempt's served questions
        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();

        var totalPoints = await context.Questions
            .AsNoTracking()
            .Where(q => questionOrder.Contains(q.Id))
            .SumAsync(q => q.Points, ct);

        attempt.ResolveViolationAndOverrideGrade(command.NewScore, totalPoints, command.Notes);

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

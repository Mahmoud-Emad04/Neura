using MediatR;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAnalytics.GetExamViolationById;

internal sealed class GetExamViolationByIdHandler(ApplicationDbContext context)
    : IRequestHandler<GetExamViolationByIdQuery, Result<ExamViolationResponse>>
{
    public async Task<Result<ExamViolationResponse>> Handle(
        GetExamViolationByIdQuery query, CancellationToken ct)
    {
        var violation = await context.ExamViolations
            .AsNoTracking()
            .Include(v => v.Student)
            .FirstOrDefaultAsync(v => v.Id == query.ViolationId && v.ExamId == query.ExamId, ct);

        if (violation is null)
            return Result.Failure<ExamViolationResponse>(ExamAttemptErrors.AttemptNotFound);

        var response = new ExamViolationResponse
        {
            Id = violation.Id,
            ExamId = violation.ExamId,
            StudentId = violation.StudentId,
            StudentName = $"{violation.Student.FirstName} {violation.Student.LastName}".Trim(),
            StudentEmail = violation.Student.Email,
            ExamAttemptId = violation.ExamAttemptId,
            Severity = violation.Severity,
            Reason = violation.Reason,
            DetectedAt = violation.DetectedAt,
            CausedAutoSubmit = violation.CausedAutoSubmit,
            FrameImagePath = violation.FrameImagePath,
            CreatedOn = violation.CreatedOn
        };

        return Result.Success(response);
    }
}

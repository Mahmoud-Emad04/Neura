using MediatR;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAnalytics.GetExamViolations;

internal sealed class GetExamViolationsHandler(ApplicationDbContext context)
    : IRequestHandler<GetExamViolationsQuery, Result<ExamViolationsListResponse>>
{
    public async Task<Result<ExamViolationsListResponse>> Handle(
        GetExamViolationsQuery query, CancellationToken ct)
    {
        var exam = await context.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.ExamId, ct);

        if (exam is null)
            return Result.Failure<ExamViolationsListResponse>(ExamErrors.ExamNotFound);

        var violations = await context.ExamViolations
            .AsNoTracking()
            .Include(v => v.Student)
            .Where(v => v.ExamId == query.ExamId)
            .OrderByDescending(v => v.DetectedAt)
            .Select(v => new ExamViolationResponse
            {
                Id = v.Id,
                ExamId = v.ExamId,
                StudentId = v.StudentId,
                StudentName = $"{v.Student.FirstName} {v.Student.LastName}".Trim(),
                StudentEmail = v.Student.Email,
                ExamAttemptId = v.ExamAttemptId,
                Severity = v.Severity,
                Reason = v.Reason,
                DetectedAt = v.DetectedAt,
                CausedAutoSubmit = v.CausedAutoSubmit,
                FrameImagePath = v.FrameImagePath,
                CreatedOn = v.CreatedOn
            })
            .ToListAsync(ct);

        var response = new ExamViolationsListResponse
        {
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            TotalCount = violations.Count,
            Violations = violations
        };

        return Result.Success(response);
    }
}

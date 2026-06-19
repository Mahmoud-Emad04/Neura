using MediatR;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.ExamAnalytics.GetExamViolations;

internal sealed class GetExamViolationsHandler(ApplicationDbContext context, IServiceHelpers serviceHelpers)
    : IRequestHandler<GetExamViolationsQuery, Result<ExamViolationsListResponse>>
{
    public async Task<Result<ExamViolationsListResponse>> Handle(
        GetExamViolationsQuery query, CancellationToken ct)
    {
        var exam = await context.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.LessonId == query.ExamId, ct);

        if (exam is null)
            return Result.Failure<ExamViolationsListResponse>(ExamErrors.ExamNotFound);

        string baseUrl = serviceHelpers.GetBaseUrl();

        var violations = await context.ExamViolations
            .AsNoTracking()
            .Include(v => v.Student)
            .Where(v => v.LessonId == exam.LessonId && !v.IsDeleted)
            .OrderByDescending(v => v.DetectedAt)
            .Select(v => new ExamViolationResponse
            {
                Id = v.Id,
                ExamId = v.LessonId,
                StudentId = v.StudentId,
                StudentName = $"{v.Student.FirstName} {v.Student.LastName}".Trim(),
                StudentEmail = v.Student.Email,
                ExamAttemptId = v.ExamAttemptId,
                Severity = v.Severity,
                Reason = v.Reason,
                DetectedAt = v.DetectedAt,
                CausedAutoSubmit = v.CausedAutoSubmit,
                FrameImagePath = string.IsNullOrEmpty(v.FrameImagePath) ? null : $"{baseUrl}/{v.FrameImagePath}",
                CreatedOn = v.CreatedOn
            })
            .ToListAsync(ct);

        var response = new ExamViolationsListResponse
        {
            ExamId = exam.LessonId,
            ExamTitle = exam.Title,
            TotalCount = violations.Count,
            Violations = violations
        };

        return Result.Success(response);
    }
}

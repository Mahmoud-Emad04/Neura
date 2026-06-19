using MediatR;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.ExamAnalytics.GetExamViolationById;

internal sealed class GetExamViolationByIdHandler(ApplicationDbContext context, IServiceHelpers serviceHelpers)
    : IRequestHandler<GetExamViolationByIdQuery, Result<ExamViolationResponse>>
{
    public async Task<Result<ExamViolationResponse>> Handle(
        GetExamViolationByIdQuery query, CancellationToken ct)
    {
        var violation = await context.ExamViolations
            .AsNoTracking()
            .Include(v => v.Student)
            .FirstOrDefaultAsync(v => v.Id == query.ViolationId && v.LessonId == query.ExamId, ct);

        if (violation is null)
            return Result.Failure<ExamViolationResponse>(ExamAttemptErrors.AttemptNotFound);

        string baseUrl = serviceHelpers.GetBaseUrl();

        var response = new ExamViolationResponse
        {
            Id = violation.Id,
            ExamId = violation.LessonId,
            StudentId = violation.StudentId,
            StudentName = $"{violation.Student.FirstName} {violation.Student.LastName}".Trim(),
            StudentEmail = violation.Student.Email,
            ExamAttemptId = violation.ExamAttemptId,
            Severity = violation.Severity,
            Reason = violation.Reason,
            DetectedAt = violation.DetectedAt,
            CausedAutoSubmit = violation.CausedAutoSubmit,
            FrameImagePath = string.IsNullOrEmpty(violation.FrameImagePath) ? null : $"{baseUrl}/{violation.FrameImagePath}",
            CreatedOn = violation.CreatedOn
        };

        return Result.Success(response);
    }
}

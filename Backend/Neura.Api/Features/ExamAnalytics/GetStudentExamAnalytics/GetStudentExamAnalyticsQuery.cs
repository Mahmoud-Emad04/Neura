using MediatR;
using Neura.Core.Contracts.Analytics;

namespace Neura.Api.Features.ExamAnalytics.GetStudentExamAnalytics;

public sealed record GetStudentExamAnalyticsQuery(int ExamId, string UserId)
    : IRequest<Result<StudentExamAnalyticsResponse>>;

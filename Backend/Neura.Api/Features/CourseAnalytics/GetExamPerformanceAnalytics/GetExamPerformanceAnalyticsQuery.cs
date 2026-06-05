using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Analytics;

namespace Neura.Api.Features.CourseAnalytics.GetExamPerformanceAnalytics;

public sealed record GetExamPerformanceAnalyticsQuery(
    string CourseKeyId,
    string UserId,
    DateOnly? From = null,
    DateOnly? To = null)
    : IRequest<Result<ExamSummaryAnalytics>>;

using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Analytics;

namespace Neura.Api.Features.ExamAnalytics.GetExamAnalytics;

public sealed record GetExamAnalyticsQuery(int ExamId, string UserId)
    : IRequest<Result<ExamAnalyticsResponse>>;

using MediatR;
using Neura.Core.Contracts.Analytics;

namespace Neura.Api.Features.CourseAnalytics.GetProgressAnalytics;

public sealed record GetProgressAnalyticsQuery(
    string CourseKeyId,
    string UserId,
    DateOnly? From = null,
    DateOnly? To = null)
    : IRequest<Result<ProgressAnalytics>>;

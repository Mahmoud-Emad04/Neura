using MediatR;
using Neura.Core.Contracts.Analytics;

namespace Neura.Api.Features.CourseAnalytics.GetEnrollmentAnalytics;

public sealed record GetEnrollmentAnalyticsQuery(
    string CourseKeyId,
    string UserId,
    DateOnly? From = null,
    DateOnly? To = null)
    : IRequest<Result<EnrollmentAnalytics>>;

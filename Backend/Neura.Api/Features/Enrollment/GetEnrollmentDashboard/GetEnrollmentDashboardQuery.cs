using MediatR;
using Neura.Core.Contracts.Enrollment;

namespace Neura.Api.Features.Enrollment.GetEnrollmentDashboard;

public sealed record GetEnrollmentDashboardQuery(string UserId)
    : IRequest<Result<EnrollmentDashboardResponse>>;

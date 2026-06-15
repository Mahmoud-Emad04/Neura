using MediatR;
using Neura.Core.Contracts.Enrollment;

namespace Neura.Api.Features.Enrollment.GetEnrollmentStatus;

public sealed record GetEnrollmentStatusQuery(string CourseIdKey, string? UserId)
    : IRequest<Result<EnrollmentStatusResponse>>;

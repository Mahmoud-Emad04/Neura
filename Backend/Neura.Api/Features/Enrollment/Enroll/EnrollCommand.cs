using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Enrollment;

namespace Neura.Api.Features.Enrollment.Enroll;

public sealed record EnrollCommand(string CourseId, string UserId) 
    : IRequest<Result<EnrollmentResponse>>;

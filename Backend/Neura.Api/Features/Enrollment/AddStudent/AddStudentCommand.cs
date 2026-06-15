using MediatR;
using Neura.Core.Contracts.Enrollment;

namespace Neura.Api.Features.Enrollment.AddStudent;

public sealed record AddStudentCommand(int CourseId, string RequesterId, string Email)
    : IRequest<Result<EnrollmentResponse>>;

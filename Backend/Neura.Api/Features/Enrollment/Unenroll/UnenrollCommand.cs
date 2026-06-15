using MediatR;

namespace Neura.Api.Features.Enrollment.Unenroll;

public sealed record UnenrollCommand(int CourseId, string UserId) : IRequest<Result>;

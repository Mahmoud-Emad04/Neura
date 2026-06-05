using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Enrollment.Unenroll;

public sealed record UnenrollCommand(int CourseId, string UserId) : IRequest<Result>;

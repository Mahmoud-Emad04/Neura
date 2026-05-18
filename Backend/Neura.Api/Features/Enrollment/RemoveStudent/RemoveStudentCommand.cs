using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Enrollment.RemoveStudent;

public sealed record RemoveStudentCommand(int CourseId, string StudentId, string RequesterId) 
    : IRequest<Result>;

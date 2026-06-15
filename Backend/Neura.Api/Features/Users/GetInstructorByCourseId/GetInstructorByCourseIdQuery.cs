using MediatR;
using Neura.Core.Contracts.Instructor;

namespace Neura.Api.Features.Users.GetInstructorByCourseId;

public sealed record GetInstructorByCourseIdQuery(string CourseId)
    : IRequest<Result<InstructorSummaryResponse>>;

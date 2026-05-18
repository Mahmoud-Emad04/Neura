using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Enrollment;

namespace Neura.Api.Features.Enrollment.GetMyTeachingCourses;

public sealed record GetMyTeachingCoursesQuery(string UserId) 
    : IRequest<Result<List<MyEnrolledCourseResponse>>>;

using MediatR;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Enrollment;

namespace Neura.Api.Features.Enrollment.GetMyEnrolledCourses;

public sealed record GetMyEnrolledCoursesQuery(string UserId, RequestFilters Filters)
    : IRequest<Result<PaginatedList<MyEnrolledCourseResponse>>>;

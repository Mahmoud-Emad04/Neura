using MediatR;
using Neura.Core.Contracts.common;

namespace Neura.Api.Features.Courses.GetBookmarkedCourses;

public sealed record GetBookmarkedCoursesQuery(RequestFilters Filters, string UserId)
    : IRequest<Result<PaginatedList<CourseSummaryResponse>>>;

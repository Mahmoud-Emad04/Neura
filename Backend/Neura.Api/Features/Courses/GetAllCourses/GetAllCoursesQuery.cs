using MediatR;
using Neura.Core.Contracts.common;

namespace Neura.Api.Features.Courses.GetAllCourses;

public sealed record GetAllCoursesQuery(RequestFilters Filters, string? UserId)
    : IRequest<Result<PaginatedList<CourseSummaryResponse>>>;

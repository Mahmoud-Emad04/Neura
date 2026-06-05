using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.GetAllCourses;

public sealed record GetAllCoursesQuery(RequestFilters Filters, string? UserId) 
    : IRequest<Result<PaginatedList<CourseSummaryResponse>>>;

using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Specification;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Courses;
using Neura.Core.Specifications.Courses;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.GetBookmarkedCourses;

internal sealed class GetBookmarkedCoursesHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers) 
    : IRequestHandler<GetBookmarkedCoursesQuery, Result<PaginatedList<CourseSummaryResponse>>>
{
    public async Task<Result<PaginatedList<CourseSummaryResponse>>> Handle(
        GetBookmarkedCoursesQuery request, CancellationToken ct)
    {
        var filters = request.Filters;
        var spec = new BookmarkedCoursesFilterSpecification(request.UserId, filters);

        var query = SpecificationEvaluator.GetQuery(context.CourseBookmarks.AsNoTracking(), spec);

        var projectedQuery = query.ProjectToType<CourseSummaryResponse>();

        var baseUrl = helpers.GetBaseUrl();

        var paginatedCourses = await PaginatedList<CourseSummaryResponse>.CreateAsync(
            projectedQuery,
            filters.PageNumber,
            filters.PageSize,
            c => c.ImageUrl = $"{baseUrl}/{c.ImageUrl}",
            ct
        );

        return Result.Success(paginatedCourses);
    }
}

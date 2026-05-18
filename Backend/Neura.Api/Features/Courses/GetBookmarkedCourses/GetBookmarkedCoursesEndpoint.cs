using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.common;
using System.Security.Claims;

namespace Neura.Api.Features.Courses.GetBookmarkedCourses;

public sealed class GetBookmarkedCoursesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses/bookmarked", async (
            int? pageNumber,
            int? pageSize,
            string? searchValue,
            string? sortColumn,
            string? sortDirection,
            bool? isFree,
            bool? isBookmarked,
            Neura.Core.Enums.CourseStatus? courseStatus,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var filters = new RequestFilters
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 10,
                SearchValue = searchValue,
                SortColumn = sortColumn,
                SortDirection = sortDirection ?? "ASC",
                IsFree = isFree,
                IsBookmarked = isBookmarked,
                CourseStatus = courseStatus
            };

            var query = new GetBookmarkedCoursesQuery(filters, userId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Courses")
        .WithName("GetBookmarkedCourses");
    }
}

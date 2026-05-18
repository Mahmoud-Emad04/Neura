using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.common;

namespace Neura.Api.Features.Courses.GetAllCourses;

public sealed class GetAllCoursesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("courses", async (
            int? pageNumber,
            int? pageSize,
            string? searchValue,
            string? sortColumn,
            string? sortDirection,
            bool? isFree,
            bool? isBookmarked,
            Neura.Core.Enums.CourseStatus? courseStatus,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.Identity?.IsAuthenticated == true 
                ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                : null;

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

            var query = new GetAllCoursesQuery(filters, userId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .WithTags("Courses")
        .WithName("GetAllCourses")
        .AllowAnonymous();
    }
}

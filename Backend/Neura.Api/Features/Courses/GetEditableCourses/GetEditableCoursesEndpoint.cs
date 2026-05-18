using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;
using Neura.Core.Enums;
using System.Security.Claims;

namespace Neura.Api.Features.Courses.GetEditableCourses;

public sealed class GetEditableCoursesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/courses/my/editable", async (
            int? pageNumber,
            int? pageSize,
            string? searchTerm,
            Neura.Core.Enums.CourseStatus? status,
            EditableRoleFilter? roleFilter,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var filters = new EditableCourseFilters
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 10,
                SearchTerm = searchTerm,
                Status = status,
                RoleFilter = roleFilter ?? EditableRoleFilter.All
            };

            var query = new GetEditableCoursesQuery(filters, userId);
            var result = await sender.Send(query, ct);

            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : result.ToProblemMinimal();
        })
        .RequireAuthorization()
        .WithTags("Courses")
        .WithName("GetEditableCourses");
    }
}

using MediatR;
using Neura.Core.Abstractions.Specification;
using Neura.Core.Enums;
using Neura.Core.Specifications.Courses;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.GetAllCourses;

internal sealed class GetAllCoursesHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetAllCoursesQuery, Result<PaginatedList<CourseSummaryResponse>>>
{
    public async Task<Result<PaginatedList<CourseSummaryResponse>>> Handle(
        GetAllCoursesQuery request, CancellationToken ct)
    {
        var filters = request.Filters;
        var spec = new CourseFilterSpecification(filters);

        var query = SpecificationEvaluator.GetQuery(context.Courses.AsNoTracking().Where(c => c.Status != CourseStatus.Pending), spec);

        var projectedQuery = query.ProjectToType<CourseSummaryResponse>();

        var baseUrl = helpers.GetBaseUrl();

        var paginatedCourses = await PaginatedList<CourseSummaryResponse>.CreateAsync(
            projectedQuery,
            filters.PageNumber,
            filters.PageSize,
            c => c.ImageUrl = $"{baseUrl}/{c.ImageUrl}",
            ct
        );

        foreach (var course in paginatedCourses.Items)
        {
            if (TryDecodeCourseId(course.KeyId, out var courseId))
            {
                var lessons = await context.Lessons
                    .Where(l => l.Section.CourseId == courseId && !l.IsDeleted)
                    .Select(l => new { l.Duration })
                    .ToListAsync(ct);

                course.NumberOfStudents = await context.CourseUsers
                    .CountAsync(c => c.CourseId == courseId && !c.IsDeleted, ct);

                course.NumberOfLessons = lessons.Count;
                course.Hours = (int)lessons.Sum(l => l.Duration.TotalHours);
            }
        }

        if (request.UserId is not null)
        {
            var bookmarkedCourseIds = await context.CourseBookmarks
                .Where(b => b.UserId == request.UserId && !b.IsDeleted)
                .Select(b => b.CourseId)
                .ToListAsync(ct);

            var courseIds = paginatedCourses.Items
                .Select(c => helpers.DecodeHash(c.KeyId).FirstOrDefault())
                .Where(id => id != 0)
                .ToList();

            var enrolledCourseIds = await context.CourseUsers
                .Where(cu => courseIds.Contains(cu.CourseId) && cu.UserId == request.UserId && !cu.IsDeleted)
                .Select(cu => cu.CourseId)
                .ToHashSetAsync(ct);

            foreach (var course in paginatedCourses.Items)
            {
                if (TryDecodeCourseId(course.KeyId, out var id))
                {
                    course.IsBookmarked = bookmarkedCourseIds.Contains(id);
                    course.IsEnrolled = enrolledCourseIds.Contains(id);
                }
            }
        }

        return Result.Success(paginatedCourses);
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }
        courseId = numbers[0];
        return true;
    }
}

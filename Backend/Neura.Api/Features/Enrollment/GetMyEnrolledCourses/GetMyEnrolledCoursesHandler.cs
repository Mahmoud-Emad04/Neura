using MediatR;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Enums;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Enrollment.GetMyEnrolledCourses;

internal sealed class GetMyEnrolledCoursesHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetMyEnrolledCoursesQuery, Result<PaginatedList<MyEnrolledCourseResponse>>>
{
    public async Task<Result<PaginatedList<MyEnrolledCourseResponse>>> Handle(
        GetMyEnrolledCoursesQuery request, CancellationToken ct)
    {
        var baseUrl = helpers.GetBaseUrl();
        var filters = request.Filters;

        var enrollments = context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.Course)
            .ThenInclude(c => c.CreatedBy)
            .Include(cu => cu.CourseRole)
            .Where(cu =>
                cu.UserId == request.UserId &&
                !cu.IsDeleted &&
                !cu.Course.IsDeleted &&
                cu.CourseRole.Level == (int)CourseRoleType.Student
                && (string.IsNullOrEmpty(filters.SearchValue) || ((cu.Course.Title.Contains(filters.SearchValue) || cu.Course.Description.Contains(filters.SearchValue) || (!string.IsNullOrEmpty(cu.Course.DisplayInstructorName) && cu.Course.DisplayInstructorName.Contains(filters.SearchValue)))))
                && (filters.CourseStatus == null || (filters.CourseStatus == cu.Course.Status)))
            .Select(cu => new MyEnrolledCourseResponse
            {
                KeyId = helpers.Encode(cu.CourseId),
                Title = cu.Course.Title,
                CourseDescription = cu.Course.Description,
                ImageUrl = $"{baseUrl}/{cu.Course.ImageUrl}",
                InstructorName = $"{cu.Course.CreatedBy.FirstName} {cu.Course.CreatedBy.LastName}",
                Role = (CourseRoleType)cu.CourseRole.Level,
                RoleName = cu.CourseRole.Name,
                IsTeamMember = false,
                IsOwner = false,
                EnrolledOn = cu.EnrolledOn,
                LastAccessedOn = cu.LastAccessedOn,
                ProgressPercentage = null,
                TotalLessons = context.Lessons.Count(l => l.Section.CourseId == cu.CourseId && !l.IsDeleted && !l.Section.IsDeleted),
                CompletedLessons = 0,
                NumberOfLessons = context.Lessons.Count(l => l.Section.CourseId == cu.CourseId && !l.IsDeleted && !l.Section.IsDeleted),
                Hours = 0,
                Price = cu.Course.Price,
                Rating = cu.Course.Rating,
                IsBookmarked = context.CourseBookmarks.Any(cb => cb.CourseId == cu.CourseId && cb.UserId == request.UserId && !cb.IsDeleted)
            })
            .OrderByDescending(cu => cu.LastAccessedOn ?? cu.EnrolledOn);

        var paginatedCourses = await PaginatedList<MyEnrolledCourseResponse>.CreateAsync(
            enrollments,
            filters.PageNumber,
            filters.PageSize,
            cancellationToken: ct
        );

        if (paginatedCourses.Items.Count > 0)
        {
            var courseIds = paginatedCourses.Items
                .Select(c => helpers.DecodeHash(c.KeyId))
                .Where(ids => ids.Length > 0)
                .Select(ids => ids[0])
                .ToList();

            var durations = await context.Lessons
                .AsNoTracking()
                .Where(l => courseIds.Contains(l.Section.CourseId) && !l.IsDeleted && !l.Section.IsDeleted)
                .Select(l => new { l.Section.CourseId, l.Duration })
                .ToListAsync(ct);

            var hoursByCourse = durations
                .GroupBy(d => d.CourseId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Duration.TotalHours));

            foreach (var item in paginatedCourses.Items)
            {
                var ids = helpers.DecodeHash(item.KeyId);
                if (ids.Length > 0 && hoursByCourse.TryGetValue(ids[0], out var hours))
                    item.Hours = hours;
            }
        }

        return Result.Success(paginatedCourses);
    }
}

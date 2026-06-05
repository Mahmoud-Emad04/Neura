using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Enums;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Enrollment.GetMyTeachingCourses;

internal sealed class GetMyTeachingCoursesHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers) 
    : IRequestHandler<GetMyTeachingCoursesQuery, Result<List<MyEnrolledCourseResponse>>>
{
    public async Task<Result<List<MyEnrolledCourseResponse>>> Handle(
        GetMyTeachingCoursesQuery request, CancellationToken ct)
    {
        var teachingCourses = await context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.Course)
            .ThenInclude(c => c.CreatedBy)
            .Include(cu => cu.Course)
            .ThenInclude(c => c.Sections)
            .ThenInclude(s => s.Lessons)
            .Include(cu => cu.CourseRole)
            .Where(cu =>
                cu.UserId == request.UserId &&
                !cu.IsDeleted &&
                !cu.Course.IsDeleted &&
                cu.CourseRole.Level >= (int)CourseRoleType.Assistant)
            .OrderByDescending(cu => cu.CourseRole.Level)
            .ThenByDescending(cu => cu.EnrolledOn)
            .ToListAsync(ct);

        var responses = teachingCourses.Select(cu => new MyEnrolledCourseResponse
        {
            KeyId = helpers.Encode(cu.CourseId),
            Title = cu.Course.Title,
            CourseDescription = cu.Course.Description,
            ImageUrl = cu.Course.ImageUrl,
            InstructorName = $"{cu.Course.CreatedBy.FirstName} {cu.Course.CreatedBy.LastName}",
            Role = (CourseRoleType)cu.CourseRole.Level,
            RoleName = cu.CourseRole.Name,
            IsTeamMember = cu.CourseRole.Level >= (int)CourseRoleType.Assistant,
            IsOwner = cu.CourseRole.Level == (int)CourseRoleType.CourseOwner,
            EnrolledOn = cu.EnrolledOn,
            LastAccessedOn = cu.LastAccessedOn,
            ProgressPercentage = null,
            TotalLessons = cu.Course.Sections.SelectMany(s => s.Lessons).Count(),
            CompletedLessons = 0,
            NumberOfLessons = cu.Course.Sections.SelectMany(s => s.Lessons).Count(),
            Hours = cu.Course.Sections
                .SelectMany(s => s.Lessons)
                .Sum(l => l.Duration.TotalHours),
            Price = cu.Course.Price,
            Rating = cu.Course.Rating,
            IsBookmarked = context.CourseBookmarks.Any(cb => cb.CourseId == cu.CourseId && cb.UserId == request.UserId && !cb.IsDeleted)
        }).ToList();

        return Result.Success(responses);
    }
}

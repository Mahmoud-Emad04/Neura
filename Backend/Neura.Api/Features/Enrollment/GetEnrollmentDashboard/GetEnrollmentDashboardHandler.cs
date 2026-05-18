using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Enums;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Enrollment.GetEnrollmentDashboard;

internal sealed class GetEnrollmentDashboardHandler(ApplicationDbContext context) 
    : IRequestHandler<GetEnrollmentDashboardQuery, Result<EnrollmentDashboardResponse>>
{
    public async Task<Result<EnrollmentDashboardResponse>> Handle(
        GetEnrollmentDashboardQuery request, CancellationToken ct)
    {
        var enrolledCourseIds = await context.CourseUsers
            .AsNoTracking()
            .Where(cu =>
                cu.UserId == request.UserId &&
                !cu.IsDeleted &&
                !cu.Course.IsDeleted &&
                cu.CourseRole.Level == (int)CourseRoleType.Student)
            .Select(cu => cu.CourseId)
            .ToListAsync(ct);

        var totalCourses = enrolledCourseIds.Count;

        if (totalCourses == 0)
        {
            return Result.Success(new EnrollmentDashboardResponse
            {
                TotalCourses = 0, CompletedCourses = 0, InProgressCourses = 0, TotalHours = 0
            });
        }

        var totalLessonsByCourse = await context.Lessons
            .AsNoTracking()
            .Where(l => enrolledCourseIds.Contains(l.Section.CourseId) && !l.IsDeleted && !l.Section.IsDeleted)
            .GroupBy(l => l.Section.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var completedLessonsByCourse = await context.LessonCompletions
            .AsNoTracking()
            .Where(lc => lc.UserId == request.UserId && enrolledCourseIds.Contains(lc.Lesson.Section.CourseId) && !lc.Lesson.IsDeleted && !lc.Lesson.Section.IsDeleted)
            .GroupBy(lc => lc.Lesson.Section.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalLessonsDict = totalLessonsByCourse.ToDictionary(x => x.CourseId, x => x.Count);
        var completedLessonsDict = completedLessonsByCourse.ToDictionary(x => x.CourseId, x => x.Count);

        var completedCourses = 0;
        var inProgressCourses = 0;

        foreach (var courseId in enrolledCourseIds)
        {
            var total = totalLessonsDict.GetValueOrDefault(courseId, 0);
            var completed = completedLessonsDict.GetValueOrDefault(courseId, 0);

            if (total > 0 && completed >= total)
                completedCourses++;
            else if (completed > 0)
                inProgressCourses++;
        }

        var durations = await context.Lessons
            .AsNoTracking()
            .Where(l => enrolledCourseIds.Contains(l.Section.CourseId) && !l.IsDeleted && !l.Section.IsDeleted)
            .Select(l => l.Duration)
            .ToListAsync(ct);

        var totalHours = durations.Sum(d => d.TotalHours);

        return Result.Success(new EnrollmentDashboardResponse
        {
            TotalCourses = totalCourses,
            CompletedCourses = completedCourses,
            InProgressCourses = inProgressCourses,
            TotalHours = Math.Round(totalHours, 2)
        });
    }
}

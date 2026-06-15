using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Enrollment.GetCourseStudents;

internal sealed class GetCourseStudentsHandler(ApplicationDbContext context)
    : IRequestHandler<GetCourseStudentsQuery, Result<CourseStudentsListResponse>>
{
    public async Task<Result<CourseStudentsListResponse>> Handle(
        GetCourseStudentsQuery request, CancellationToken ct)
    {
        var course = await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId && !c.IsDeleted, ct);

        if (course is null) return Result.Failure<CourseStudentsListResponse>(EnrollmentErrors.CourseNotFound);

        var requester = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == request.CourseId && cu.UserId == request.RequesterId && !cu.IsDeleted, ct);

        if (requester is null || !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ViewAnalytics))
            return Result.Failure<CourseStudentsListResponse>(EnrollmentErrors.CannotRemoveStudent);

        var studentsQuery = context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.CourseRole)
            .Where(cu => cu.CourseId == request.CourseId && !cu.IsDeleted && cu.CourseRole.Level == (int)CourseRoleType.Student)
            .OrderByDescending(cu => cu.EnrolledOn);

        var totalStudents = await studentsQuery.CountAsync(ct);

        var students = await studentsQuery
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(cu => new CourseStudentResponse
            {
                UserId = cu.UserId,
                FirstName = cu.User.FirstName,
                LastName = cu.User.LastName,
                Email = cu.User.Email ?? string.Empty,
                EnrolledOn = cu.EnrolledOn,
                LastAccessedOn = cu.LastAccessedOn,
                ProgressPercentage = null,
                CompletedLessons = 0
            })
            .ToListAsync(ct);

        return Result.Success(new CourseStudentsListResponse
        {
            CourseId = request.CourseId,
            CourseName = course.Title,
            TotalStudents = totalStudents,
            MaxStudents = null,
            Students = students
        });
    }
}

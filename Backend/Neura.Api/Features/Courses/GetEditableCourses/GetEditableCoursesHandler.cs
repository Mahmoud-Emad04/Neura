using MediatR;
using Neura.Core.Contracts.Courses;
using Neura.Core.Enums;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.GetEditableCourses;

internal sealed class GetEditableCoursesHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetEditableCoursesQuery, Result<EditableCoursesListSummaryResponse>>
{
    public async Task<Result<EditableCoursesListSummaryResponse>> Handle(
        GetEditableCoursesQuery request, CancellationToken ct)
    {
        var filters = request.Filters;
        var userId = request.UserId;

        var ownerLevel = (int)CourseRoleType.CourseOwner;
        var coInstructorLevel = (int)CourseRoleType.CoInstructor;
        var studentLevel = (int)CourseRoleType.Student;

        var query = context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .Include(cu => cu.Course)
            .Where(cu =>
                cu.UserId == userId &&
                !cu.IsDeleted &&
                cu.CourseRole.Level >= coInstructorLevel)
            .Select(cu => new
            {
                CourseUser = cu,
                cu.Course,
                RoleLevel = cu.CourseRole.Level,
                IsOwner = cu.CourseRole.Level == ownerLevel,
                IsCoInstructor = cu.CourseRole.Level == coInstructorLevel
            })
            .Where(x => !x.Course.IsDeleted);

        query = filters.RoleFilter switch
        {
            EditableRoleFilter.OwnedOnly => query.Where(x => x.IsOwner),
            EditableRoleFilter.CoInstructorOnly => query.Where(x => x.IsCoInstructor),
            _ => query
        };

        if (filters.Status.HasValue)
            query = query.Where(x => x.Course.Status == filters.Status.Value);

        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var searchTerm = filters.SearchTerm.ToLower().Trim();
            query = query.Where(x =>
                x.Course.Title.ToLower().Contains(searchTerm) ||
                (x.Course.Description != null && x.Course.Description.ToLower().Contains(searchTerm)));
        }

        var totalOwnedCourses = await context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .CountAsync(cu =>
                    cu.UserId == userId &&
                    !cu.IsDeleted &&
                    !cu.Course.IsDeleted &&
                    cu.CourseRole.Level == ownerLevel,
                ct);

        var totalCoInstructorCourses = await context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .CountAsync(cu =>
                    cu.UserId == userId &&
                    !cu.IsDeleted &&
                    !cu.Course.IsDeleted &&
                    cu.CourseRole.Level == coInstructorLevel,
                ct);

        var totalCount = await query.CountAsync(ct);

        var courseIds = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(x => x.Course.Id)
            .ToListAsync(ct);

        var coursesData = await query
            .Where(x => courseIds.Contains(x.Course.Id))
            .Select(x => new
            {
                x.Course,
                x.RoleLevel,
                x.IsOwner,
                x.IsCoInstructor
            })
            .ToListAsync(ct);

        var studentCounts = await context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .Where(cu =>
                courseIds.Contains(cu.CourseId) &&
                !cu.IsDeleted &&
                cu.CourseRole.Level == studentLevel)
            .GroupBy(cu => cu.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, ct);

        var baseUrl = helpers.GetBaseUrl();

        var items = coursesData.Select(x =>
        {
            var course = x.Course;
            var isOwner = x.IsOwner;
            var isCoInstructor = x.IsCoInstructor;

            studentCounts.TryGetValue(course.Id, out var studentCount);

            return new EditableCourseSummaryResponse
            {
                KeyId = helpers.Encode(course.Id),
                Title = course.Title,
                ImageUrl = Path.Combine(baseUrl, course.ImageUrl),
                Status = course.Status,
                StatusName = course.Status.ToString(),
                InstructorName = course.DisplayInstructorName!,
                IsEnrollmentOpen = course.IsEnrollmentOpen,
                RoleName = isOwner ? "Owner" : "Co-Instructor",
                IsOwner = isOwner,
                IsCoInstructor = isCoInstructor,
                NumberOfStudents = studentCount,
                CreatedOn = course.CreatedOn,
                UpdatedOn = course.UpdatedOn,
                AvailableActions = BuildAvailableActions(course.Status, isOwner, isCoInstructor)
            };
        }).ToList();

        var orderedItems = courseIds
            .Select(id => items.First(i => TryDecodeCourseId(i.KeyId, out var decodedId) && decodedId == id))
            .ToList();

        var paginatedList = new PaginatedList<EditableCourseSummaryResponse>(
            orderedItems,
            filters.PageNumber,
            totalCount,
            filters.PageSize);

        return Result.Success(new EditableCoursesListSummaryResponse
        {
            TotalOwnedCourses = totalOwnedCourses,
            TotalCoInstructorCourses = totalCoInstructorCourses,
            Courses = paginatedList
        });
    }

    private static CourseAvailableActions BuildAvailableActions(
        CourseStatus status,
        bool isOwner,
        bool isCoInstructor)
    {
        return new CourseAvailableActions
        {
            CanEdit = true,
            CanAddSections = true,
            CanAddLessons = true,
            CanDelete = isOwner,
            CanManageStudents = isOwner,
            CanManageInstructors = isOwner,
            CanActivate = isOwner && status == CourseStatus.Pending,
            CanComplete = isOwner && status == CourseStatus.Active,
            CanReactivate = isOwner && status == CourseStatus.Completed,
            CanUnpublish = isOwner && status == CourseStatus.Active
        };
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

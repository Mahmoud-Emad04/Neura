using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Enums;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EnrollmentService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IServiceHelpers _helpers;
    public EnrollmentService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<EnrollmentService> logger, IServiceHelpers helpers)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _helpers = helpers;
    }

    public async Task<Result<EnrollmentResponse>> EnrollAsync(string keyId, string userId)
    {
        // Get user
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.UserNotFound);

        // Check email verification
        if (!user.EmailConfirmed) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.EmailNotVerified);

        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<EnrollmentResponse>(CourseErrors.CourseNotFound);

        // Get course
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

        if (course is null) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.CourseNotFound);

        // Check if course is active/published
        if (!course.IsPubliclyVisible) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.CourseNotActive);

        // Check if already enrolled
        var existingEnrollment = await _context.CourseUsers
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId);

        if (existingEnrollment is not null)
        {
            if (!existingEnrollment.IsDeleted)
                return Result.Failure<EnrollmentResponse>(EnrollmentErrors.AlreadyEnrolled);

            // Reactivate soft-deleted enrollment
            existingEnrollment.IsDeleted = false;
            existingEnrollment.EnrolledOn = DateTime.UtcNow;
            existingEnrollment.LastAccessedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} re-enrolled in course {CourseId}", userId, courseId);

            return Result.Success(await MapToEnrollmentResponseAsync(existingEnrollment, course, user));
        }

        // Check if course requires payment (for future implementation)
        if (course.Price > 0)
            // For now, we'll allow enrollment but log it
            // In future, this would redirect to payment flow
            _logger.LogWarning(
                "User {UserId} enrolling in paid course {CourseId} without payment (payment not implemented)",
                userId, courseId);

        // Get student role
        var studentRole = await _context.CourseRoles
            .FirstAsync(r => r.Level == (int)CourseRoleType.Student);

        // Create enrollment
        var courseUser = new CourseUser
        {
            CourseId = courseId,
            UserId = userId,
            CourseRoleId = studentRole.Id,
            PermissionMask = studentRole.PermissionMask,
            EnrolledOn = DateTime.UtcNow,
            LastAccessedOn = DateTime.UtcNow
        };

        _context.CourseUsers.Add(courseUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} enrolled in course {CourseId}", userId, courseId);

        return Result.Success(await MapToEnrollmentResponseAsync(courseUser, course, user));
    }

    public async Task<Result> UnenrollAsync(int courseId, string userId)
    {
        var courseUser = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == userId &&
                !cu.IsDeleted);

        if (courseUser is null) return Result.Failure(EnrollmentErrors.NotEnrolled);

        // Cannot unenroll if owner
        if (courseUser.CourseRole.Level == (int)CourseRoleType.CourseOwner)
            return Result.Failure(EnrollmentErrors.CannotUnenrollOwner);

        // Cannot unenroll if team member (must be removed by owner)
        if (courseUser.CourseRole.Level >= (int)CourseRoleType.Assistant)
            return Result.Failure(EnrollmentErrors.CannotUnenrollTeamMember);

        // Soft delete
        courseUser.IsDeleted = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} unenrolled from course {CourseId}", userId, courseId);

        return Result.Success();
    }

    public async Task<Result<EnrollmentStatusResponse>> GetEnrollmentStatusAsync(string keyId, string userId)
    {
        if (TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<EnrollmentStatusResponse>(EnrollmentErrors.CourseNotFound);

        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

        if (course is null) return Result.Failure<EnrollmentStatusResponse>(EnrollmentErrors.CourseNotFound);

        var user = await _userManager.FindByIdAsync(userId);

        var courseUser = await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == userId &&
                !cu.IsDeleted);

        var isEnrolled = courseUser is not null;
        var canEnroll = !isEnrolled && course.IsPubliclyVisible;
        string? cannotEnrollReason = null;

        if (!canEnroll && !isEnrolled)
            if (!course.IsPubliclyVisible)
                cannotEnrollReason = "This course is not currently available";

        if (user is not null && !user.EmailConfirmed && !isEnrolled)
        {
            canEnroll = false;
            cannotEnrollReason = "Please verify your email before enrolling";
        }

        return Result.Success(new EnrollmentStatusResponse
        {
            IsEnrolled = isEnrolled,
            CanEnroll = canEnroll,
            CannotEnrollReason = cannotEnrollReason,
            CourseId = keyId,
            CourseName = course.Title,
            IsFree = course.Price == 0,
            Price = course.Price,
            Currency = "USD", // Default currency
            CurrentRole = courseUser is not null ? (CourseRoleType)courseUser.CourseRole.Level : null,
            CurrentRoleName = courseUser?.CourseRole.Name,
            EnrolledOn = courseUser?.EnrolledOn
        });
    }

    public async Task<Result<PaginatedList<MyEnrolledCourseResponse>>> GetMyEnrolledCoursesAsync(string userId, RequestFilters requestFilters, CancellationToken cancellationToken)
    {
        var enrollments = _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.Course)
            .ThenInclude(c => c.CreatedBy)
            .Include(cu => cu.CourseRole)
            .Where(cu =>
                cu.UserId == userId &&
                !cu.IsDeleted &&
                !cu.Course.IsDeleted &&
                cu.CourseRole.Level == (int)CourseRoleType.Student
                && (string.IsNullOrEmpty(requestFilters.SearchValue) || ((cu.Course.Title.Contains(requestFilters.SearchValue) || cu.Course.Description.Contains(requestFilters.SearchValue) || (!string.IsNullOrEmpty(cu.Course.DisplayInstructorName) && cu.Course.DisplayInstructorName.Contains(requestFilters.SearchValue)))))
                && (requestFilters.CourseStatus == null || (requestFilters.CourseStatus == cu.Course.Status)))

            .Select(cu => new MyEnrolledCourseResponse
            {
                CourseId = _helpers.Encode(cu.CourseId),
                CourseName = cu.Course.Title,
                CourseDescription = cu.Course.Description,
                CourseThumbnail = cu.Course.ImageUrl,
                InstructorName = $"{cu.Course.CreatedBy.FirstName} {cu.Course.CreatedBy.LastName}",
                Role = (CourseRoleType)cu.CourseRole.Level,
                RoleName = cu.CourseRole.Name,
                IsTeamMember = false,
                IsOwner = false,
                EnrolledOn = cu.EnrolledOn,
                LastAccessedOn = cu.LastAccessedOn,
                // Progress tracking would be implemented separately
                ProgressPercentage = null,
                TotalLessons = 0,
                CompletedLessons = 0
            })
            .OrderByDescending(cu => cu.LastAccessedOn ?? cu.EnrolledOn);

        var paginatedCourses = await PaginatedList<MyEnrolledCourseResponse>.CreateAsync(
            enrollments,
            requestFilters.PageNumber,
            requestFilters.PageSize,
            cancellationToken: cancellationToken
        );

        return Result.Success(paginatedCourses);
    }

    public async Task<Result<List<MyEnrolledCourseResponse>>> GetMyTeachingCoursesAsync(string userId)
    {
        var teachingCourses = await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.Course)
            .ThenInclude(c => c.CreatedBy)
            .Include(cu => cu.CourseRole)
            .Where(cu =>
                cu.UserId == userId &&
                !cu.IsDeleted &&
                !cu.Course.IsDeleted &&
                cu.CourseRole.Level >= (int)CourseRoleType.Assistant)
            .OrderByDescending(cu => cu.CourseRole.Level)
            .ThenByDescending(cu => cu.EnrolledOn)
            .ToListAsync();

        var responses = teachingCourses.Select(cu => new MyEnrolledCourseResponse
        {
            CourseId = _helpers.Encode(cu.CourseId),
            CourseName = cu.Course.Title,
            CourseDescription = cu.Course.Description,
            CourseThumbnail = cu.Course.ImageUrl,
            InstructorName = $"{cu.Course.CreatedBy.FirstName} {cu.Course.CreatedBy.LastName}",
            Role = (CourseRoleType)cu.CourseRole.Level,
            RoleName = cu.CourseRole.Name,
            IsTeamMember = cu.CourseRole.Level >= (int)CourseRoleType.Assistant,
            IsOwner = cu.CourseRole.Level == (int)CourseRoleType.CourseOwner,
            EnrolledOn = cu.EnrolledOn,
            LastAccessedOn = cu.LastAccessedOn,
            ProgressPercentage = null,
            TotalLessons = 0,
            CompletedLessons = 0
        }).ToList();

        return Result.Success(responses);
    }

    public async Task<Result<CourseStudentsListResponse>> GetCourseStudentsAsync(
        int courseId,
        string requesterId,
        int pageNumber = 1,
        int pageSize = 20)
    {
        // Check if course exists
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

        if (course is null) return Result.Failure<CourseStudentsListResponse>(EnrollmentErrors.CourseNotFound);

        // Check requester has permission
        var requester = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == requesterId &&
                !cu.IsDeleted);

        if (requester is null ||
            !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ViewAnalytics))
            return Result.Failure<CourseStudentsListResponse>(EnrollmentErrors.CannotRemoveStudent);

        // Get students (only those with Student role)
        var studentsQuery = _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.CourseRole)
            .Where(cu =>
                cu.CourseId == courseId &&
                !cu.IsDeleted &&
                cu.CourseRole.Level == (int)CourseRoleType.Student)
            .OrderByDescending(cu => cu.EnrolledOn);

        var totalStudents = await studentsQuery.CountAsync();

        var students = await studentsQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
            .ToListAsync();

        return Result.Success(new CourseStudentsListResponse
        {
            CourseId = courseId,
            CourseName = course.Title,
            TotalStudents = totalStudents,
            MaxStudents = null, // Can be implemented if needed
            Students = students
        });
    }

    public async Task<Result<EnrollmentResponse>> AddStudentAsync(
        int courseId,
        AddStudentRequest request,
        string requesterId)
    {
        // Check course exists
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

        if (course is null) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.CourseNotFound);

        // Check requester has permission
        var requester = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == requesterId &&
                !cu.IsDeleted);

        if (requester is null ||
            !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageStudents))
            return Result.Failure<EnrollmentResponse>(EnrollmentErrors.CannotRemoveStudent);

        // Find user by email
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user is null) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.UserNotFound);

        // Check if already enrolled
        var existingEnrollment = await _context.CourseUsers
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == user.Id);

        if (existingEnrollment is not null && !existingEnrollment.IsDeleted)
            return Result.Failure<EnrollmentResponse>(EnrollmentErrors.AlreadyEnrolled);

        // Get student role
        var studentRole = await _context.CourseRoles
            .FirstAsync(r => r.Level == (int)CourseRoleType.Student);

        if (existingEnrollment is not null)
        {
            // Reactivate
            existingEnrollment.IsDeleted = false;
            existingEnrollment.EnrolledOn = DateTime.UtcNow;
            existingEnrollment.EnrolledById = requesterId;
            existingEnrollment.CourseRoleId = studentRole.Id;
            existingEnrollment.PermissionMask = studentRole.PermissionMask;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {RequesterId} re-added student {UserId} to course {CourseId}",
                requesterId, user.Id, courseId);

            return Result.Success(await MapToEnrollmentResponseAsync(existingEnrollment, course, user));
        }

        // Create new enrollment
        var courseUser = new CourseUser
        {
            CourseId = courseId,
            UserId = user.Id,
            CourseRoleId = studentRole.Id,
            PermissionMask = studentRole.PermissionMask,
            EnrolledOn = DateTime.UtcNow,
            EnrolledById = requesterId
        };

        _context.CourseUsers.Add(courseUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {RequesterId} added student {UserId} to course {CourseId}",
            requesterId, user.Id, courseId);

        return Result.Success(await MapToEnrollmentResponseAsync(courseUser, course, user));
    }

    public async Task<Result> RemoveStudentAsync(int courseId, string studentId, string requesterId)
    {
        // Check requester has permission
        var requester = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == requesterId &&
                !cu.IsDeleted);

        if (requester is null ||
            !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageStudents))
            return Result.Failure(EnrollmentErrors.CannotRemoveStudent);

        // Find student
        var student = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == studentId &&
                !cu.IsDeleted);

        if (student is null) return Result.Failure(EnrollmentErrors.StudentNotFound);

        // Can only remove students (not team members)
        if (student.CourseRole.Level > (int)CourseRoleType.Student)
            return Result.Failure(EnrollmentErrors.CannotUnenrollTeamMember);

        // Soft delete
        student.IsDeleted = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {RequesterId} removed student {StudentId} from course {CourseId}",
            requesterId, studentId, courseId);

        return Result.Success();
    }

    public async Task UpdateLastAccessedAsync(int courseId, string userId)
    {
        var courseUser = await _context.CourseUsers
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == userId &&
                !cu.IsDeleted);

        if (courseUser is not null)
        {
            courseUser.LastAccessedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    #region Private Helpers

    private async Task<EnrollmentResponse> MapToEnrollmentResponseAsync(
        CourseUser courseUser,
        Course course,
        ApplicationUser user)
    {
        var role = courseUser.CourseRole ?? await _context.CourseRoles
            .FirstAsync(r => r.Id == courseUser.CourseRoleId);

        return new EnrollmentResponse
        {
            CourseId = course.Id,
            CourseName = course.Title,
            CourseThumbnail = course.ImageUrl,
            UserId = user.Id,
            UserName = $"{user.FirstName} {user.LastName}",
            Role = (CourseRoleType)role.Level,
            RoleName = role.Name,
            EnrolledOn = courseUser.EnrolledOn,
            LastAccessedOn = courseUser.LastAccessedOn,
            IsActive = !courseUser.IsDeleted
        };
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = _helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }

        courseId = numbers[0];
        return true;
    }


    #endregion
}
using Neura.Core.Abstractions;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Enrollment;

namespace Neura.Core.Services;

public interface IEnrollmentService
{
    /// <summary>
    ///     Enroll current user in a course
    /// </summary>
    Task<Result<EnrollmentResponse>> EnrollAsync(string keyId, string userId);

    /// <summary>
    ///     Unenroll current user from a course
    /// </summary>
    Task<Result> UnenrollAsync(int courseId, string userId);

    /// <summary>
    ///     Check enrollment status for a user in a course
    /// </summary>
    Task<Result<EnrollmentStatusResponse>> GetEnrollmentStatusAsync(string courseId, string userId);

    /// <summary>
    ///     Get all courses user is enrolled in
    /// </summary>
    Task<Result<PaginatedList<MyEnrolledCourseResponse>>> GetMyEnrolledCoursesAsync(string userId, RequestFilters requestFilters, CancellationToken cancellationToken);

    /// <summary>
    ///     Get courses user is teaching (as owner or team member)
    /// </summary>
    Task<Result<List<MyEnrolledCourseResponse>>> GetMyTeachingCoursesAsync(string userId);

    /// <summary>
    ///     Get students enrolled in a course (for instructors)
    /// </summary>
    Task<Result<CourseStudentsListResponse>> GetCourseStudentsAsync(
        int courseId,
        string requesterId,
        int pageNumber = 1,
        int pageSize = 20);

    /// <summary>
    ///     Add a student to a course (by instructor)
    /// </summary>
    Task<Result<EnrollmentResponse>> AddStudentAsync(int courseId, AddStudentRequest request, string requesterId);

    /// <summary>
    ///     Remove a student from a course (by instructor)
    /// </summary>
    Task<Result> RemoveStudentAsync(int courseId, string studentId, string requesterId);

    /// <summary>
    ///     Update last accessed timestamp
    /// </summary>
    Task UpdateLastAccessedAsync(int courseId, string userId);
}
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Enums;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    /// <summary>
    ///     Enroll in a course
    /// </summary>
    [HttpPost("{courseId}/enroll")]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll(string courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _enrollmentService.EnrollAsync(courseId, userId);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetEnrollmentStatus), new { courseId }, result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Unenroll from a course
    /// </summary>
    [HttpPost("{courseId:int}/unenroll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unenroll(int courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _enrollmentService.UnenrollAsync(courseId, userId);

        return result.IsSuccess
            ? Ok(new { message = "Successfully unenrolled from course" })
            : result.ToProblem();
    }

    /// <summary>
    ///     Get enrollment status for a course
    /// </summary>
    [HttpGet("{courseId}/enrollment-status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EnrollmentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollmentStatus(string courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            // Return basic info for anonymous users
            return Ok(new EnrollmentStatusResponse
            {
                IsEnrolled = false,
                CanEnroll = false,
                CannotEnrollReason = "Please sign in to enroll",
                CourseId = courseId
            });

        var result = await _enrollmentService.GetEnrollmentStatusAsync(courseId, userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Get my enrolled courses (as student)
    /// </summary>
    [HttpGet("/api/courses/enrolled")]
    [ProducesResponseType(typeof(PaginatedList<MyEnrolledCourseResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrolledCourses([FromQuery] RequestFilters requestFilters, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _enrollmentService.GetMyEnrolledCoursesAsync(userId, requestFilters, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Get courses I'm teaching (as instructor/team member)
    /// </summary>
    [HttpGet("/api/courses/teaching")]
    [ProducesResponseType(typeof(List<MyEnrolledCourseResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTeachingCourses()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _enrollmentService.GetMyTeachingCoursesAsync(userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Get students enrolled in a course (for instructors)
    /// </summary>
    [HttpGet("{courseId:int}/students")]
    [HasCoursePermission(CoursePermission.ViewAnalytics)]
    [ProducesResponseType(typeof(CourseStudentsListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseStudents(
        int courseId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _enrollmentService.GetCourseStudentsAsync(courseId, userId, pageNumber, pageSize);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Add a student to a course (by instructor)
    /// </summary>
    [HttpPost("{courseId:int}/students")]
    [HasCoursePermission(CoursePermission.ManageStudents)]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddStudent(int courseId, [FromBody] AddStudentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _enrollmentService.AddStudentAsync(courseId, request, userId);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetCourseStudents), new { courseId }, result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Remove a student from a course (by instructor)
    /// </summary>
    [HttpDelete("{courseId:int}/students/{studentId}")]
    [HasCoursePermission(CoursePermission.ManageStudents)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStudent(int courseId, string studentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _enrollmentService.RemoveStudentAsync(courseId, studentId, userId);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
}
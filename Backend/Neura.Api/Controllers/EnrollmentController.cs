using MediatR;
using Neura.Api.Features.Enrollment.AddStudent;
using Neura.Api.Features.Enrollment.Enroll;
using Neura.Api.Features.Enrollment.GetCourseStudents;
using Neura.Api.Features.Enrollment.GetEnrollmentDashboard;
using Neura.Api.Features.Enrollment.GetEnrollmentStatus;
using Neura.Api.Features.Enrollment.GetMyEnrolledCourses;
using Neura.Api.Features.Enrollment.GetMyTeachingCourses;
using Neura.Api.Features.Enrollment.RemoveStudent;
using Neura.Api.Features.Enrollment.Unenroll;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Enums;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class EnrollmentController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Enroll in a course
    /// </summary>
    [HttpPost("{courseId}/enroll")]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll(string courseId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new EnrollCommand(courseId, userId);
        var result = await sender.Send(command, ct);

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
    public async Task<IActionResult> Unenroll(int courseId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new UnenrollCommand(courseId, userId);
        var result = await sender.Send(command, ct);

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
    public async Task<IActionResult> GetEnrollmentStatus(string courseId, CancellationToken ct)
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

        var query = new GetEnrollmentStatusQuery(courseId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Get my enrolled courses (as student)
    /// </summary>
    [HttpGet("/api/courses/enrolled")]
    [ProducesResponseType(typeof(PaginatedList<MyEnrolledCourseResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrolledCourses([FromQuery] RequestFilters requestFilters, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetMyEnrolledCoursesQuery(userId, requestFilters);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Get courses I'm teaching (as instructor/team member)
    /// </summary>
    [HttpGet("/api/courses/teaching")]
    [ProducesResponseType(typeof(List<MyEnrolledCourseResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTeachingCourses(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetMyTeachingCoursesQuery(userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
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
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetCourseStudentsQuery(courseId, userId, pageNumber, pageSize);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
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
    public async Task<IActionResult> AddStudent(int courseId, [FromBody] AddStudentRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new AddStudentCommand(courseId, userId, request.Email);
        var result = await sender.Send(command, ct);

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
    public async Task<IActionResult> RemoveStudent(int courseId, string studentId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new RemoveStudentCommand(courseId, studentId, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
    /// <summary>
    ///     Get enrollment dashboard summary (total, completed, in-progress, hours)
    /// </summary>
    [HttpGet("/api/courses/enrollment-dashboard")]
    [ProducesResponseType(typeof(EnrollmentDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrollmentDashboard(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetEnrollmentDashboardQuery(userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
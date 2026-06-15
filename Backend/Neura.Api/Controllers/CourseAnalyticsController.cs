using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.CourseAnalytics.GetEnrollmentAnalytics;
using Neura.Api.Features.CourseAnalytics.GetExamPerformanceAnalytics;
using Neura.Api.Features.CourseAnalytics.GetProgressAnalytics;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Enums;

namespace Neura.Api.Controllers;

[Route("api/courses/{courseId}/analytics")]
[ApiController]
[Authorize]
[HasCoursePermission(CoursePermission.ViewAnalytics)]
public class CourseAnalyticsController(ISender sender) : ControllerBase
{
    // ══════════════════════════════════════════════════════════════════════════
    //  GET /api/courses/{courseId}/analytics/enrollment
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Returns enrollment analytics for a course.
    /// </summary>
    /// <remarks>
    ///     Includes total students, active students, new enrollments (this week / this month),
    ///     and a daily enrollment trend chart.
    ///     The <c>newThisWeek</c> and <c>newThisMonth</c> convenience fields are only populated
    ///     when no custom date range is supplied.
    ///     Optional date-range filter via <c>?from</c> and <c>?to</c> (format: yyyy-MM-dd, inclusive).
    ///     When omitted, all-time totals and the last 30-day trend are returned.
    /// </remarks>
    /// <response code="200">Returns the enrollment analytics.</response>
    /// <response code="403">Caller does not have ViewAnalytics permission on this course.</response>
    /// <response code="404">Course not found or invalid courseId.</response>
    [HttpGet("enrollment")]
    [ProducesResponseType(typeof(EnrollmentAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrollmentAnalytics(
        [FromRoute] string courseId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId()!;
        var result = await sender.Send(
            new GetEnrollmentAnalyticsQuery(courseId, userId, from, to), ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  GET /api/courses/{courseId}/analytics/progress
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Returns student progress analytics for a course.
    /// </summary>
    /// <remarks>
    ///     Includes total / published lesson counts, average completion percentage,
    ///     count of students who finished 100% of lessons, and a completion distribution
    ///     breakdown (0-25%, 26-50%, 51-75%, 76-99%, 100%).
    ///     Optional date-range filter via <c>?from</c> and <c>?to</c> (format: yyyy-MM-dd, inclusive)
    ///     scopes the lesson completion records used for the calculation.
    ///     Lesson counts are always all-time structural data regardless of the filter.
    /// </remarks>
    /// <response code="200">Returns the progress analytics.</response>
    /// <response code="403">Caller does not have ViewAnalytics permission on this course.</response>
    /// <response code="404">Course not found or invalid courseId.</response>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(ProgressAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgressAnalytics(
        [FromRoute] string courseId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId()!;
        var result = await sender.Send(
            new GetProgressAnalyticsQuery(courseId, userId, from, to), ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  GET /api/courses/{courseId}/analytics/exams
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Returns exam performance analytics aggregated across all exams in a course.
    /// </summary>
    /// <remarks>
    ///     Includes the total exam count, overall average score and pass rate
    ///     (averaged across all exams that have attempts), and a per-exam breakdown
    ///     (attempt count, average score %, pass rate).
    ///     Optional date-range filter via <c>?from</c> and <c>?to</c> (format: yyyy-MM-dd, inclusive)
    ///     scopes which exam attempts are included in the aggregation.
    /// </remarks>
    /// <response code="200">Returns the exam performance analytics.</response>
    /// <response code="403">Caller does not have ViewAnalytics permission on this course.</response>
    /// <response code="404">Course not found or invalid courseId.</response>
    [HttpGet("exams")]
    [ProducesResponseType(typeof(ExamSummaryAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamPerformanceAnalytics(
        [FromRoute] string courseId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId()!;
        var result = await sender.Send(
            new GetExamPerformanceAnalyticsQuery(courseId, userId, from, to), ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}

using Neura.Api.Extensions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Courses;
using Neura.Core.Contracts.Files;
using Neura.Services.Filters;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CoursesController(ICourseService courseService, ILogger<CoursesController> logger) : ControllerBase
{
    private readonly ICourseService _courseService = courseService;
    private readonly ILogger<CoursesController> _logger = logger;

    // ====================
    // READ OPERATIONS
    // ====================

    /// <summary>
    ///     Retrieves a paginated list of courses based on dynamic filters.
    /// </summary>
    /// <remarks>
    ///     Anonymous access is allowed. If the user is authenticated, bookmark and enrollment
    ///     status will be populated for each course.
    /// </remarks>
    /// <param name="filters">Pagination, search, and sorting parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of course summaries.</returns>
    /// <response code="200">Returns the paginated course list.</response>
    [ProducesResponseType(typeof(PaginatedList<CourseSummaryResponse>), StatusCodes.Status200OK)]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] RequestFilters filters, CancellationToken cancellationToken)
    {
        return Ok((await _courseService.GetAllAsync(filters, User.GetUserId(), cancellationToken)).Value);
    }

    /// <summary>
    ///     Retrieves the content (sections and lessons) for a specific course.
    /// </summary>
    /// <remarks>
    ///     Returns the course's sections and lessons hierarchy.
    ///     Provide the public HashId (e.g., "Xy7zK"), not the integer database ID.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The course content including sections and lessons.</returns>
    /// <response code="200">Returns the course content.</response>
    /// <response code="404">Course not found or HashId is invalid.</response>
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpGet("{courseId}/content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetContentByIdAsync(courseId, User.GetUserId(), cancellationToken);

        return course.IsSuccess
            ? Ok(course.Value)
            : course.ToProblem();
    }

    /// <summary>
    ///     Retrieves metadata (details, tags, stats) for a specific course.
    /// </summary>
    /// <remarks>
    ///     Returns course info, tags, learning outcomes, prerequisites, enrollment count,
    ///     and the caller's enrollment/bookmark/owner status.
    ///     Provide the public HashId (e.g., "Xy7zK"), not the integer database ID.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The course metadata.</returns>
    /// <response code="200">Returns the course metadata.</response>
    /// <response code="404">Course not found or HashId is invalid.</response>
    [ProducesResponseType(typeof(CourseMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpGet("{courseId}/metadata")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMetadataById([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetCourseMetadataAsync(courseId, User.GetUserId(), cancellationToken);

        return course.IsSuccess
            ? Ok(course.Value)
            : course.ToProblem();
    }

    /// <summary>
    ///     Retrieves all courses the current user is enrolled in.
    /// </summary>
    /// <remarks>
    ///     Returns only active enrollments (excludes soft-deleted ones).
    ///     Courses where the user is the owner are excluded.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of enrolled course metadata.</returns>
    /// <response code="200">Returns the list of enrolled courses.</response>
    [HttpGet("my-learning")]
    [EndpointSummary("Get my enrolled courses")]
    [ProducesResponseType(typeof(IEnumerable<CourseMetadataResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseMetadataResponse>>> GetMyLearning(
        CancellationToken cancellationToken)
    {
        var result = await _courseService.GetEnrolledCoursesAsync(User.GetUserId()!, cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{courseId}/status")]
    [Authorize]
    //[HasCoursePermission(Permissions.ViewCourseStatus)]
    public async Task<IActionResult> GetStatus(
        string courseId,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.GetCourseStatusAsync(courseId, User.GetUserId()!, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ====================================
    // WRITE OPERATIONS (Create / Update)
    // ====================================

    /// <summary>
    ///     Creates a new course and assigns the creator as the owner.
    /// </summary>
    /// <remarks>
    ///     The authenticated user is automatically added as the course owner.
    ///     A <c>Location</c> header pointing to the content endpoint is returned on success.
    /// </remarks>
    /// <param name="request">The course creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A 201 response with the Location header of the new course.</returns>
    /// <response code="201">Course created successfully.</response>
    /// <response code="400">Validation failed (e.g., invalid or duplicate tags).</response>
    [ProducesResponseType(typeof(CourseMetadataResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [HttpPost("")]
    public async Task<IActionResult> Create([FromForm] CourseRequest request, CancellationToken cancellationToken)
    {
        var result = await _courseService.CreateAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMetadataById), new { courseId = result.Value.KeyId }, result.Value.KeyId)
            : result.ToProblem();
    }

    /// <summary>
    ///     Updates the details of a course (title, description, tags, dates, outcomes, prerequisites).
    /// </summary>
    /// <remarks>
    ///     Requires <see cref="Permissions.UpdateCourses" /> permission on the course.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Course updated successfully.</response>
    /// <response code="404">Course or one or more tags not found.</response>
    [ProducesResponseType(typeof(Error), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpPut("{courseId}")]
    [HasCoursePermission(Permissions.UpdateCourses)]
    public async Task<IActionResult> UpdateDetails([FromRoute] string courseId, [FromBody] CourseUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateAsync(courseId, request,
            User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }


    /// <summary>
    ///     Uploads or replaces the cover image for a course.
    /// </summary>
    /// <remarks>
    ///     Accepts <c>multipart/form-data</c>. The previous image is deleted automatically
    ///     unless it is the default placeholder.
    ///     Requires <see cref="Permissions.UpdateCourses" /> permission on the course.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="uploadImage">The form file containing the new image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Image updated successfully.</response>
    /// <response code="404">Course not found.</response>
    [ProducesResponseType(typeof(Error), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpPut("{courseId}/cover-image")]
    [HasCoursePermission(Permissions.UpdateCourses)]
    public async Task<IActionResult> UpdateImage([FromRoute] string courseId, [FromForm] UploadImageRequest uploadImage,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateImageAsync(courseId, uploadImage,
            User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    /// <summary>
    /// Gets all courses the current user can edit (as Owner or Co-Instructor).
    /// </summary>
    /// <remarks>
    /// Returns courses where the user has CourseOwner or CoInstructor permissions.
    /// Includes stats like student count, lesson count, and available actions.
    /// </remarks>
    [HttpGet("my/editable")]
    [Authorize]
    [ProducesResponseType(typeof(EditableCoursesListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEditableCourses(
        [FromQuery] EditableCourseFilters filters,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.GetEditableCoursesAsync(
            filters,
            User.GetUserId()!,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // ============================
    // ACTIONS (Enroll / Bookmark)
    // ============================

    /// <summary>
    ///     Enrolls the current user in a specific course.
    /// </summary>
    /// <remarks>
    ///     Idempotent — if the user was previously enrolled and soft-deleted,
    ///     the enrollment is restored.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">User successfully enrolled.</response>
    /// <response code="404">Course not found.</response>
    [HttpPost("{courseId}/enroll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enroll([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var result = await _courseService.EnrollAsync(courseId, User.GetUserId()!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }


    /// <summary>
    ///     Unenrolls the current user from a course.
    /// </summary>
    /// <remarks>
    ///     The enrollment is soft-deleted.
    ///     Course owners cannot unenroll — they must delete the course or transfer ownership.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Successfully unenrolled.</response>
    /// <response code="400">The user is the course owner.</response>
    /// <response code="404">The user is not enrolled in this course.</response>
    [HttpDelete("{courseId}/enroll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unenroll([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var result = await _courseService.UnenrollAsync(courseId, User.GetUserId()!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    /// <summary>
    ///     Toggles the bookmark status for a specific course.
    /// </summary>
    /// <remarks>
    ///     Adds the bookmark if it does not exist; removes it if it does.
    ///     Soft-deleted bookmarks are restored automatically.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Bookmark status toggled successfully.</response>
    /// <response code="404">Course not found.</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpPost("{courseId}/bookmark")]
    public async Task<IActionResult> ToggleBookmark([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var result = await _courseService.ToggleBookmarkAsync(courseId, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Retrieves a paginated list of courses bookmarked by the current user.
    /// </summary>
    /// <param name="filters">Pagination, search, and sorting parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of bookmarked course summaries.</returns>
    /// <response code="200">Returns the bookmarked courses.</response>
    [ProducesResponseType(typeof(PaginatedList<CourseSummaryResponse>), StatusCodes.Status200OK)]
    [HttpGet("bookmarked")]
    public async Task<IActionResult> GetBookmarked([FromQuery] RequestFilters filters,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.GetBookmarkedAsync(User.GetUserId()!, filters, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ══════════════════════════════════════════════════════════════
    // Commands — Status Transitions
    // ══════════════════════════════════════════════════════════════

    [HttpPost("{courseId}/activate")]
    [Authorize]
    //[HasCoursePermission(Permissions.ManageCourseStatus)]
    public async Task<IActionResult> Activate(
        string courseId,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.ActivateCourseAsync(courseId, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("{courseId}/complete")]
    [Authorize]
    //[HasCoursePermission(Permissions.ManageCourseStatus)]
    public async Task<IActionResult> Complete(
        string courseId,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.CompleteCourseAsync(courseId, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("{courseId}/reactivate")]
    [Authorize]
    //[HasCoursePermission(Permissions.ManageCourseStatus)]
    public async Task<IActionResult> Reactivate(
        string courseId,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.ReactivateCourseAsync(courseId, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("{courseId}/unpublish")]
    [Authorize]
    //[HasCoursePermission(Permissions.ManageCourseStatus)]
    public async Task<IActionResult> Unpublish(
        string courseId,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.UnpublishCourseAsync(courseId, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}
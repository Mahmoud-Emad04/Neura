using System.Security.Claims;
using Neura.Api.Extensions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Files;
using Neura.Services.Filters;

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
    ///     Route: GET /api/courses
    /// </summary>
    /// <param name="filters">The pagination, search, and sorting parameters to apply to the query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of courses.</returns>
    [ProducesResponseType(typeof(PaginatedList<CourseResponse>), StatusCodes.Status200OK)]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] RequestFilters filters, CancellationToken cancellationToken)
    {
        return Ok((await _courseService.GetAllAsync(filters, User.GetUserId(), cancellationToken)).Value);
    }

    /// <summary>
    ///     Retrieves a specific course by its hashed ID.
    ///     Route: GET /api/courses/{courseId}
    /// </summary>
    /// <remarks>
    ///     ⚠️ **Important:** Provide the public string HashId (e.g., "Xy7zK"), not the integer database ID.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detailed information of the course.</returns>
    /// <response code="200">Returns the requested course</response>
    /// <response code="404">If the course HashId is invalid or does not exist</response>
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpGet("{courseId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetByIdAsync(courseId, cancellationToken);

        return course.IsSuccess
            ? Ok(course.Value)
            : course.ToProblem();
    }

    /// <summary>
    ///     Retrieves all courses the current user is currently learning.
    ///     Route: GET /api/courses/enrolled
    /// </summary>
    /// <remarks>
    ///     Returns only active enrollments (excludes soft-deleted ones).
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of enrolled courses.</response>
    [HttpGet("my-learning")]
    [EndpointSummary("Get my enrolled courses")]
    [ProducesResponseType(typeof(IEnumerable<CourseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseResponse>>> GetMyLearning(CancellationToken cancellationToken)
    {
        var result = await _courseService.GetEnrolledCoursesAsync(User.GetUserId()!, cancellationToken);

        return Ok(result.Value);
    }


    // ====================================
    // WRITE OPERATIONS (Create / Update)
    // ====================================
    /// <summary>
    ///     Creates a new course and assigns the creator as the Owner.
    ///     Route: POST /api/courses
    /// </summary>
    /// <remarks>
    ///     The user creating the course is automatically added to the `CourseUsers` table with 'Owner' permissions.
    /// </remarks>
    /// <param name="request">The course creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created course with its new HashId.</returns>
    /// <response code="201">Returns the newly created course</response>
    /// <response code="400">If validation fails (e.g. invalid tags)</response>
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] CourseRequest request, CancellationToken cancellationToken)
    {
        var result = await _courseService.CreateAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { courseId = result.Value.KeyId }, null)
            : result.ToProblem();
    }

    /// <summary>
    ///     Updates the text details (Title, Description, Tags) of a course.
    ///     Route: PUT /api/courses/{courseId}
    /// </summary>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Course updated successfully</response>
    /// <response code="404">Course or Tags not found</response>
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
    ///     Uploads or updates the cover image for a course.
    ///     Route: PUT /api/courses/{courseId}/cover-image
    /// </summary>
    /// <remarks>
    ///     Requires `multipart/form-data`. The old image is deleted if it exists.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="uploadImage">The form file containing the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Image updated successfully</response>
    /// <response code="404">Course not found</response>
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


    // ============================
    // ACTIONS (Enroll / Bookmark)
    // ============================

    /// <summary>
    ///     Enrolls the current user in a specific course.
    ///     Route: POST /api/courses/{courseId}/enrollment
    /// </summary>
    /// <remarks>
    ///     This is an idempotent operation. If the user was previously enrolled (and soft-deleted),
    ///     this restores their access.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">User successfully enrolled (no content returned).</response>
    /// <response code="404">Course not found.</response>
    [HttpPost("enroll/{courseId}")]
    [EndpointSummary("Enroll in a course")]
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
    ///     Route: DELETE /api/courses/{courseId}/enrollment
    /// </summary>
    /// <remarks>
    ///     - **Students:** The enrollment is soft-deleted.
    ///     - **Owners:** Cannot unenroll (must delete course or transfer ownership).
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="204">Successfully unenrolled.</response>
    /// <response code="400">If the user is the Owner.</response>
    /// <response code="404">If the user is not enrolled.</response>
    [HttpDelete("unenroll/{courseId}")]
    [EndpointSummary("Unenroll from a course")]
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
    ///     Route: POST /api/courses/{courseId}/bookmark
    /// </summary>
    /// <remarks>
    ///     If the course is currently bookmarked, it will be removed.
    ///     If it is not bookmarked, it will be added.
    ///     This endpoint handles soft-deleted bookmarks automatically.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Bookmark status successfully toggled.</response>
    /// <response code="404">Course not found.</response>
    [EndpointSummary("Toggle course bookmark")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpPost("/bookmark/{courseId}")]
    public async Task<IActionResult> ToggleBookmark(string courseId, CancellationToken cancellationToken)
    {
        var result = await _courseService.ToggleBookmarkAsync(courseId, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
using Neura.Api.Extensions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Files;
using Neura.Services.Filters;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CourseController(ICourseService courseService, ILogger<CourseController> logger) : ControllerBase
{
    private readonly ICourseService _courseService = courseService;
    private readonly ILogger<CourseController> _logger = logger;

    /// <summary>
    ///     Retrieves a paginated list of courses based on dynamic filters.
    /// </summary>
    /// <param name="filters">The pagination, search, and sorting parameters to apply to the query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of courses.</returns>
    [ProducesResponseType(typeof(PaginatedList<CourseResponse>), StatusCodes.Status200OK)]
    [HttpGet("")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] RequestFilters filters, CancellationToken cancellationToken)
    {
        return Ok((await _courseService.GetAllAsync(filters, cancellationToken)).Value);
    }

    /// <summary>
    ///     Retrieves a specific course by its hashed ID.
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

    /// Creates a new course and assigns the creator as the Owner.
    /// </summary>
    /// <remarks>
    ///     The user creating the course is automatically added to the `CourseUsers` table with 'Owner' permissions.
    /// </remarks>
    /// <param name="Request">The course creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created course with its new HashId.</returns>
    /// <response code="201">Returns the newly created course</response>
    /// <response code="400">If validation fails (e.g. invalid tags)</response>
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] CourseRequest Request, CancellationToken cancellationToken)
    {
        var result = await _courseService.CreateAsync(Request, User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { courseId = result.Value.KeyId }, null)
            : result.ToProblem();
    }

    /// <summary>
    ///     Uploads or updates the cover image for a course.
    /// </summary>
    /// <remarks>
    ///     Requires `multipart/form-data`. The old image is deleted if it exists.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="UploadImage">The form file containing the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Image updated successfully</response>
    /// <response code="404">Course not found</response>
    [ProducesResponseType(typeof(Error), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpPut("update-image/{courseId}")]
    [HasCoursePermission(Permissions.UpdateCourses)]
    public async Task<IActionResult> UpdateImage([FromRoute] string courseId, [FromForm] UploadImageRequest UploadImage,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateImageAsync(courseId, UploadImage,
            User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    /// <summary>
    ///     Updates the text details (Title, Description, Tags) of a course.
    /// </summary>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="Request">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Course updated successfully</response>
    /// <response code="404">Course or Tags not found</response>
    [ProducesResponseType(typeof(Error), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [HttpPut("{courseId}")]
    [HasCoursePermission(Permissions.UpdateCourses)]
    public async Task<IActionResult> Update([FromRoute] string courseId, [FromBody] CourseUpdateRequest Request,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateAsync(courseId, Request,
            User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    /// <summary>
    ///     Enrolls the current user in a specific course.
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
    [ProducesResponseType(StatusCodes.Status204NoContent)] // Fixed: Removed typeof(Error)
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enroll([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var result = await _courseService.EnrollAsync(courseId, User.GetUserId()!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    /// <summary>
    ///     Retrieves all courses the current user is currently learning.
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

    /// <summary>
    ///     Unenrolls the current user from a course.
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




    #region TOREVIEW

    //// Need to review
    //[HttpDelete("{courseId}")]
    //[HasCoursePermission(Permissions.DeleteCourses)]
    //public async Task<IActionResult> Delete([FromRoute] string courseId, CancellationToken cancellationToken)
    //{
    //    var result = await _courseService.DeleteAsync(courseId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

    //    return result.IsSuccess
    //        ? NoContent()
    //        : result.ToProblem();
    //}

    //[HttpGet("paged")]
    ////[HasPermission(Permissions.GetCourses)]
    //public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] int? tagId = null, CancellationToken cancellationToken = default)
    //{
    //    var result = await _courseService.GetPagedAsync(page, pageSize, tagId, cancellationToken);

    //    return result.IsSuccess
    //        ? Ok(result.Value)
    //        : result.ToProblem();
    //}

    //// Admin endpoints
    //[HttpGet("deleted")]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> GetDeleted([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    //{
    //    var result = await _courseService.GetDeletedAsync(page, pageSize, cancellationToken);

    //    return result.IsSuccess
    //        ? Ok(result.Value)
    //        : result.ToProblem();
    //}

    //[HttpDelete("purge/{courseId}")]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> Purge([FromRoute] string courseId, CancellationToken cancellationToken)
    //{
    //    var result = await _courseService.PurgeAsync(courseId, cancellationToken);

    //    return result.IsSuccess
    //        ? NoContent()
    //        : result.ToProblem();
    //}

    //[HttpPut("restore/{courseId}")]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> Restore([FromRoute] string courseId, CancellationToken cancellationToken)
    //{
    //    var result = await _courseService.RestoreAsync(courseId, cancellationToken);

    //    return result.IsSuccess
    //        ? NoContent()
    //        : result.ToProblem();
    //}

    #endregion
}
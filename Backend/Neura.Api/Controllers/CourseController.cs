using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Course;
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
    /// Retrieves a list of all available courses.
    /// </summary>
    /// <remarks>
    /// This endpoint returns all courses with their associated tags. 
    /// The image URLs are fully qualified.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>A list of CourseResponse objects.</returns>
    /// 
    [ProducesResponseType(typeof(IEnumerable<CourseResponse>), StatusCodes.Status200OK)]
    [HttpGet("")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok((await _courseService.GetAllAsync(cancellationToken)).Value);
    }
    /// <summary>
    /// Retrieves a specific course by its hashed ID.
    /// </summary>
    /// <remarks>
    /// ⚠️ **Important:** Provide the public string HashId (e.g., "Xy7zK"), not the integer database ID.
    /// </remarks>
    /// <param name="courseId">The hashed string ID of the course.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detailed information of the course.</returns>
    /// <response code="200">Returns the requested course</response>
    /// <response code="404">If the course HashId is invalid or does not exist</response>
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]

    [HttpGet("{courseId}")]
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
    /// The user creating the course is automatically added to the `CourseUsers` table with 'Owner' permissions.
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
        var result = await _courseService.CreateAsync(Request, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { courseId = result.Value.KeyId }, null)
            : result.ToProblem();
    }
    /// <summary>
    /// Uploads or updates the cover image for a course.
    /// </summary>
    /// <remarks>
    /// Requires `multipart/form-data`. The old image is deleted if it exists.
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
    public async Task<IActionResult> UpdateImage([FromRoute] string courseId, [FromForm] UploadImageRequest UploadImage, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateImageAsync(courseId, UploadImage, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
    /// <summary>
    /// Updates the text details (Title, Description, Tags) of a course.
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
    public async Task<IActionResult> Update([FromRoute] string courseId, [FromBody] CourseUpdateRequest Request, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateAsync(courseId, Request, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
}
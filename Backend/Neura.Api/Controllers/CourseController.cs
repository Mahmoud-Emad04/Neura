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

    [HttpGet("")]
    //[HasPermission(Permissions.GetCourses)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok((await _courseService.GetAllAsync(cancellationToken)).Value);
    }

    [HttpGet("{courseId}")]
    //[HasCoursePermission(Permissions.GetCourses)]
    public async Task<IActionResult> GetById([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetByIdAsync(courseId, cancellationToken);

        return course.IsSuccess
            ? Ok(course.Value)
            : course.ToProblem();
    }

    [HttpPost("")]
    //[HasPermission(Permissions.AddCourses)]
    public async Task<IActionResult> Create([FromBody] CourseRequest Request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _courseService.CreateAsync(Request, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { courseId = result.Value.KeyId }, null)
            : result.ToProblem();
    }

    [HttpPut("update-image/{courseId}")]
    [HasCoursePermission(Permissions.UpdateCourses)]
    public async Task<IActionResult> UpdateImage([FromRoute] string courseId, [FromForm] UploadImageRequest UploadImage, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateImageAsync(courseId, UploadImage, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    [HttpPut("{courseId}")]
    [HasCoursePermission(Permissions.UpdateCourses)]
    public async Task<IActionResult> Update([FromRoute] string courseId, [FromBody] CourseUpdateRequest Request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _courseService.UpdateAsync(courseId, Request, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    [HttpDelete("{courseId}")]
    [HasCoursePermission(Permissions.DeleteCourses)]
    public async Task<IActionResult> Delete([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var result = await _courseService.DeleteAsync(courseId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    [HttpGet("paged")]
    //[HasPermission(Permissions.GetCourses)]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] int? tagId = null, CancellationToken cancellationToken = default)
    {
        var result = await _courseService.GetPagedAsync(page, pageSize, tagId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    // Admin endpoints
    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeleted([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _courseService.GetDeletedAsync(page, pageSize, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpDelete("purge/{courseId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Purge([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var result = await _courseService.PurgeAsync(courseId, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    [HttpPut("restore/{courseId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore([FromRoute] string courseId, CancellationToken cancellationToken)
    {
        var result = await _courseService.RestoreAsync(courseId, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
}
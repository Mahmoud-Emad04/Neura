using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Course;
using Neura.Core.Contracts.Files;
using Neura.Services.Authentication.Filters;
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
    [HasPermission(Permissions.GetCourses)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok((await _courseService.GetAllAsync(cancellationToken)).Value);
    }

    [HttpGet("{courseId}")]
    [HasPermission(Permissions.GetCourses)]
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
        var result = await _courseService.CreateAsync(Request, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { courseId = result.Value.KeyId }, null)
            : result.ToProblem();
    }

    [HttpPut("update-image/{courseId}")]
    [HasPermission(Permissions.UpdateCourses)]
    public async Task<IActionResult> UpdateImage([FromRoute] string courseId, [FromForm] UploadImageRequest UploadImage, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateImageAsync(courseId, UploadImage, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }

    [HttpPut("{courseId}")]
    [HasPermission(Permissions.UpdateCourses)]
    public async Task<IActionResult> Update([FromRoute] string courseId, [FromBody] CourseUpdateRequest Request, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateAsync(courseId, Request, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
}
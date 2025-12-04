using System.Security.Claims;
using FileManager.Contracts;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Authentication.Filters;
using Neura.Core.Contracts.Course;
using Neura.Core.Contracts.Files;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CourseController(ICourseService courseService, ILogger<CourseController> logger) : ControllerBase
{
    private readonly ICourseService _courseService = courseService;
    private readonly ILogger<CourseController> _logger = logger;

    [HttpGet("")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok((await _courseService.GetAllAsync(cancellationToken)).Value);
    }

    [HttpGet("{keyId}")]
    public async Task<IActionResult> GetById([FromRoute] string keyId, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetByIdAsync(keyId, cancellationToken);

        return course.IsSuccess
            ? Ok(course.Value)
            : course.ToProblem();
    }

    [HttpPost("")]
    [HasPermission(Permissions.AddCourses)]
    public async Task<IActionResult> Create([FromForm] CourseRequest Request, [FromForm] UploadImageRequest UploadImage, CancellationToken cancellationToken)
    {
        var result = await _courseService.CreateAsync(Request, UploadImage, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { keyId = result.Value.KeyId } , null)
            : result.ToProblem();
    }

    [HttpPut("{keyId}")]
    public async Task<IActionResult> Update([FromRoute] string keyId,[FromForm] CourseUpdateRequest Request, [FromForm] UploadImageRequest? UploadImage, CancellationToken cancellationToken)
    {
        var result = await _courseService.UpdateAsync(keyId , Request, UploadImage, User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : result.ToProblem();
    }
}
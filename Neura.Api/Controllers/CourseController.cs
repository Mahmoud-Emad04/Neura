using System.Security.Claims;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Authentication.Filters;
using Neura.Core.Contracts.Course;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CourseController(ICourseService courseService, ILogger<CourseController> logger) : ControllerBase
{
    private readonly ICourseService _courseService = courseService;
    private readonly ILogger<CourseController> _logger = logger;

    [HttpGet("")]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok((await _courseService.GetAllAsync(cancellationToken)).Value);
    }

    [HttpGet("{keyId}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string keyId, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetByIdAsync(keyId, cancellationToken);

        return course.IsSuccess
            ? Ok(course.Value)
            : course.ToProblem();
    }

    [HttpPost("")]
    [HasPermission(Permissions.AddCourses)]
    public async Task<IActionResult> CreateAsync(CourseRequest request, CancellationToken cancellationToken)
    {
        await _courseService.CreateAsync(request,User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);
        return NoContent();
    }
}
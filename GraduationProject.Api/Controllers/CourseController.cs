using System.Security.Claims;
using GraduationProject.Core.Abstractions;
using GraduationProject.Core.Contracts.Course;
using GraduationProject.Core.Service;

namespace GraduationProject.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
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
    public async Task<IActionResult> CreateAsync(CourseRequest request, CancellationToken cancellationToken)
    {
        await _courseService.CreateAsync(request,User.FindFirstValue(ClaimTypes.NameIdentifier)!, cancellationToken);
        return NoContent();
    }
}
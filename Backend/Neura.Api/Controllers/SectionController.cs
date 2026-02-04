using Neura.Core.Contracts.Section;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[Route("api/sections")]
[ApiController]
[Authorize]
public class SectionController(ISectionService sectionService, ILogger<SectionController> logger) : ControllerBase
{
	private readonly ISectionService _sectionService = sectionService;
	private readonly ILogger<SectionController> _logger = logger;

	[HttpGet("~/api/courses/{courseId}/sections")]
	[AllowAnonymous]
	public async Task<IActionResult> GetAllByCourse([FromRoute] string courseId, CancellationToken cancellationToken = default)
	{
		var result = await _sectionService.GetAllByCourseAsync(courseId, cancellationToken);
		return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
	}

	[HttpGet("{sectionId}")]
	public async Task<IActionResult> GetById([FromRoute] int sectionId, CancellationToken cancellationToken)
	{
		var result = await _sectionService.GetByIdAsync(sectionId, cancellationToken);
		return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
	}

	[HttpPost("~/api/courses/{courseId}/sections")]
	//[HasCoursePermission(Permissions.AddCourses)]
	public async Task<IActionResult> Create([FromRoute] string courseId, [FromBody] SectionRequest request, CancellationToken cancellationToken)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		var result = await _sectionService.CreateAsync(courseId, request, userId, cancellationToken);

		// This ensures the Location header returns the correct standard URL: /api/sections/{id}
		return result.IsSuccess
			? CreatedAtAction(nameof(GetById), new { sectionId = result.Value.Id }, null)
			: result.ToProblem();
	}

	[HttpPut("{sectionId}")]
	//[HasCoursePermission(Permissions.UpdateCourses)]
	public async Task<IActionResult> Update([FromRoute] int sectionId, [FromBody] SectionUpdateRequest request, CancellationToken cancellationToken)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		var result = await _sectionService.UpdateAsync(sectionId, request, userId, cancellationToken);
		return result.IsSuccess ? NoContent() : result.ToProblem();
	}

	[HttpPut("{sectionId}/status")]
	//[HasCoursePermission(Permissions.DeleteCourses)]
	public async Task<IActionResult> ToggleStatus([FromRoute] int sectionId, CancellationToken cancellationToken)
	{
		var result = await _sectionService.ToggleStatusAsync(sectionId, cancellationToken);
		return result.IsSuccess ? NoContent() : result.ToProblem();
	}
}
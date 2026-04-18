using Neura.Core.Contracts.Section;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[Route("api/sections")]
[ApiController]
[Authorize]
public class SectionsController(ISectionService sectionService) : ControllerBase
{
	private readonly ISectionService _sectionService = sectionService;

	/// <summary>
	///     Retrieves a list of sections for a specific course with optional filters.
	///     Route: GET /api/courses/{courseId}/sections
	/// </summary>
	/// <param name="courseId">The hashed string ID of the course.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A list of sections belonging to the course.</returns>
	[HttpGet("~/api/courses/{courseId}/sections")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(IEnumerable<SectionResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetAllByCourse([FromRoute] string courseId,
		CancellationToken cancellationToken = default)
	{
		var result = await _sectionService.GetAllByCourseAsync(courseId, cancellationToken);
		return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
	}

	/// <summary>
	///     Retrieves a specific section by its ID.
	///     Route: GET /api/sections/{sectionId}
	/// </summary>
	/// <param name="sectionId">The ID of the section.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The details of the requested section.</returns>
	[HttpGet("{sectionId}")]
	[ProducesResponseType(typeof(SectionResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetById([FromRoute] int sectionId, CancellationToken cancellationToken)
	{
		var result = await _sectionService.GetByIdAsync(sectionId, cancellationToken);
		return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
	}

	/// <summary>
	///     Creates a new section within a specific course.
	///     Route: POST /api/courses/{courseId}/sections
	/// </summary>
	/// <param name="courseId">The hashed string ID of the course.</param>
	/// <param name="request">The section creation payload (Title, Description, etc.).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The newly created section location.</returns>
	[HttpPost("~/api/courses/{courseId}/sections")]
	[ProducesResponseType(typeof(SectionResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Create([FromRoute] string courseId, [FromBody] SectionRequest request,
		CancellationToken cancellationToken)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		var result = await _sectionService.CreateAsync(courseId, request, userId, cancellationToken);

		// This ensures the Location header returns the correct standard URL: /api/sections/{id}
		return result.IsSuccess
			? CreatedAtAction(nameof(GetById), new { sectionId = result.Value.Id }, result.Value)
			: result.ToProblem();
	}

	/// <summary>
	///     Updates a section's details (Title, Description, Order).
	///     Route: PUT /api/sections/{sectionId}
	/// </summary>
	/// <param name="sectionId">The ID of the section to update.</param>
	/// <param name="request">The update payload.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>NoContent on success.</returns>
	[HttpPut("{sectionId}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Update([FromRoute] int sectionId, [FromBody] SectionUpdateRequest request,
		CancellationToken cancellationToken)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		var result = await _sectionService.UpdateAsync(sectionId, request, userId, cancellationToken);
		return result.IsSuccess ? NoContent() : result.ToProblem();
	}

	/// <summary>
	///     Toggles the active status of a section (Publish/Unpublish).
	///     Route: PUT /api/sections/{sectionId}/status
	/// </summary>
	/// <param name="sectionId">The ID of the section.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>NoContent on success.</returns>
	[HttpPut("{sectionId}/status")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	public async Task<IActionResult> ToggleStatus([FromRoute] int sectionId, CancellationToken cancellationToken)
	{
		var result = await _sectionService.ToggleStatusAsync(sectionId, cancellationToken);
		return result.IsSuccess ? NoContent() : result.ToProblem();
	}

	/// <summary>
	///     Soft deletes a section.
	///     Route: DELETE /api/sections/{sectionId}
	/// </summary>
	/// <param name="sectionId">The ID of the section.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>NoContent on success.</returns>
	[HttpDelete("{sectionId}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Delete([FromRoute] int sectionId, CancellationToken cancellationToken)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		var result = await _sectionService.DeleteAsync(sectionId, userId, cancellationToken);
		return result.IsSuccess ? NoContent() : result.ToProblem();
	}
}
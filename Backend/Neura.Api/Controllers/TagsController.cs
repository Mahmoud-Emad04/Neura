using Neura.Api.Extensions;
using Neura.Core.Contracts.Tags;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(
        ITagService tagService
       )
    {
        _tagService = tagService;
    }

    // ══════════════════════════════════════════════════════════════
    // Public Queries (No Auth Required)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets all active tags for selection/filtering (Public)
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<TagSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTags(CancellationToken cancellationToken)
    {
        var result = await _tagService.GetActiveTagsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Gets popular tags based on course count (Public)
    /// </summary>
    [HttpGet("popular")]
    [ProducesResponseType(typeof(IEnumerable<TagSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPopularTags(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _tagService.GetPopularTagsAsync(count, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Gets a tag by its slug (Public)
    /// </summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await _tagService.GetBySlugAsync(slug, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ══════════════════════════════════════════════════════════════
    // Admin Queries (Auth Required)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets all tags with pagination and filtering (Admin)
    /// </summary>
    [HttpGet]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] TagFilters filters,
        CancellationToken cancellationToken)
    {
        var result = await _tagService.GetAllAsync(filters, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Gets a tag by ID (Admin)
    /// </summary>
    [HttpGet("{id:int}")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _tagService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ══════════════════════════════════════════════════════════════
    // Admin Commands (Auth Required)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new tag (Admin)
    /// </summary>
    [HttpPost]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tagService.CreateAsync(request, User.GetUserId()!, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    /// <summary>
    /// Updates an existing tag (Admin)
    /// </summary>
    [HttpPut("{id:int}")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tagService.UpdateAsync(id, request, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    /// Deletes a tag (Admin)
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="force">If true, removes tag from all courses before deleting</param>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _tagService.DeleteAsync(id, force, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    /// Toggles tag active status (Admin)
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _tagService.ToggleActiveAsync(id, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ══════════════════════════════════════════════════════════════
    // Bulk Operations (Admin)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates display order for multiple tags (Admin)
    /// </summary>
    [HttpPatch("bulk/order")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkUpdateOrder(
        [FromBody] BulkUpdateTagsOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tagService.BulkUpdateOrderAsync(request, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    /// Toggles active status for multiple tags (Admin)
    /// </summary>
    [HttpPatch("bulk/toggle-active")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkToggleActive(
        [FromBody] BulkToggleTagsActiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tagService.BulkToggleActiveAsync(request, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    /// Deletes multiple tags (Admin)
    /// </summary>
    [HttpDelete("bulk")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkDelete(
        [FromBody] BulkDeleteTagsRequest request,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _tagService.BulkDeleteAsync(request, force, User.GetUserId()!, cancellationToken);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }
}

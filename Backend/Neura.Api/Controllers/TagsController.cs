using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Neura.Api.Extensions;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.Tags;
using Neura.Api.Features.Tags.BulkDeleteTags;
using Neura.Api.Features.Tags.BulkToggleTagsActive;
using Neura.Api.Features.Tags.BulkUpdateTagsOrder;
using Neura.Api.Features.Tags.CreateTag;
using Neura.Api.Features.Tags.DeleteTag;
using Neura.Api.Features.Tags.GetActiveTags;
using Neura.Api.Features.Tags.GetPopularTags;
using Neura.Api.Features.Tags.GetTagById;
using Neura.Api.Features.Tags.GetTagBySlug;
using Neura.Api.Features.Tags.GetTags;
using Neura.Api.Features.Tags.ToggleTagActive;
using Neura.Api.Features.Tags.UpdateTag;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TagsController(ISender sender) : ControllerBase
{
    // ==========================================
    // Public Queries (No Auth Required)
    // ==========================================

    /// <summary>
    ///     Gets all active tags for selection/filtering (Public)
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<TagSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTags(CancellationToken ct)
    {
        var query = new GetActiveTagsQuery();
        var result = await sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Gets popular tags based on course count (Public)
    /// </summary>
    [HttpGet("popular")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<TagSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPopularTags(
        [FromQuery] int count = 10,
        CancellationToken ct = default)
    {
        var query = new GetPopularTagsQuery(count);
        var result = await sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Gets a tag by its slug (Public)
    /// </summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        string slug,
        CancellationToken ct)
    {
        var query = new GetTagBySlugQuery(slug);
        var result = await sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    // Admin Queries (Auth Required)
    // ==========================================

    /// <summary>
    ///     Gets all tags with pagination and filtering (Admin)
    /// </summary>
    [HttpGet]
    [AdminOnly]
    [ProducesResponseType(typeof(TagListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] TagFilters filters,
        CancellationToken ct)
    {
        var query = new GetTagsQuery(filters);
        var result = await sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Gets a tag by ID (Admin)
    /// </summary>
    [HttpGet("{id:int}")]
    [AdminOnly]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
    {
        var query = new GetTagByIdQuery(id);
        var result = await sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    // Admin Commands (Auth Required)
    // ==========================================

    /// <summary>
    ///     Creates a new tag (Admin)
    /// </summary>
    [HttpPost]
    [AdminOnly]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTagRequest request,
        CancellationToken ct)
    {
        var command = new CreateTagCommand(request, User.GetUserId()!);
        var result = await sender.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    /// <summary>
    ///     Updates an existing tag (Admin)
    /// </summary>
    [HttpPut("{id:int}")]
    [AdminOnly]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTagRequest request,
        CancellationToken ct)
    {
        var command = new UpdateTagCommand(id, request, User.GetUserId()!);
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Deletes a tag (Admin)
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="force">If true, removes tag from all courses before deleting</param>
    [HttpDelete("{id:int}")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        var command = new DeleteTagCommand(id, force, User.GetUserId()!);
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Toggles tag active status (Admin)
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    [AdminOnly]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(
        int id,
        CancellationToken ct)
    {
        var command = new ToggleTagActiveCommand(id, User.GetUserId()!);
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ==========================================
    // Bulk Operations (Admin)
    // ==========================================

    /// <summary>
    ///     Updates display order for multiple tags (Admin)
    /// </summary>
    [HttpPatch("bulk/order")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkUpdateOrder(
        [FromBody] BulkUpdateTagsOrderRequest request,
        CancellationToken ct)
    {
        var command = new BulkUpdateTagsOrderCommand(request, User.GetUserId()!);
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Toggles active status for multiple tags (Admin)
    /// </summary>
    [HttpPatch("bulk/toggle-active")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkToggleActive(
        [FromBody] BulkToggleTagsActiveRequest request,
        CancellationToken ct)
    {
        var command = new BulkToggleTagsActiveCommand(request, User.GetUserId()!);
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Deletes multiple tags (Admin)
    /// </summary>
    [HttpDelete("bulk")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkDelete(
        [FromBody] BulkDeleteTagsRequest request,
        [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        var command = new BulkDeleteTagsCommand(request, force, User.GetUserId()!);
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }
}
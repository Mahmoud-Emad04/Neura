using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;

namespace Neura.Core.Services;

public interface ITagService
{
    // ══════════════════════════════════════════════════════════════
    // Queries
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    ///     Gets all tags with pagination and filtering (Admin)
    /// </summary>
    Task<Result<TagListResponse>> GetAllAsync(
        TagFilters filters,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all active tags for dropdowns/selection (Public)
    /// </summary>
    Task<Result<IEnumerable<TagSummaryResponse>>> GetActiveTagsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a tag by ID
    /// </summary>
    Task<Result<TagResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a tag by slug
    /// </summary>
    Task<Result<TagResponse>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets popular tags (most used)
    /// </summary>
    Task<Result<IEnumerable<TagSummaryResponse>>> GetPopularTagsAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    // ══════════════════════════════════════════════════════════════
    // Commands
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    ///     Creates a new tag
    /// </summary>
    Task<Result<TagResponse>> CreateAsync(
        CreateTagRequest request,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing tag
    /// </summary>
    Task<Result<TagResponse>> UpdateAsync(
        int id,
        UpdateTagRequest request,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a tag (soft delete)
    /// </summary>
    Task<Result> DeleteAsync(
        int id,
        bool forceDelete,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Toggles tag active status
    /// </summary>
    Task<Result<TagResponse>> ToggleActiveAsync(
        int id,
        string userId,
        CancellationToken cancellationToken = default);

    // ══════════════════════════════════════════════════════════════
    // Bulk Operations
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    ///     Updates display order for multiple tags
    /// </summary>
    Task<Result> BulkUpdateOrderAsync(
        BulkUpdateTagsOrderRequest request,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Toggles active status for multiple tags
    /// </summary>
    Task<Result> BulkToggleActiveAsync(
        BulkToggleTagsActiveRequest request,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes multiple tags
    /// </summary>
    Task<Result> BulkDeleteAsync(
        BulkDeleteTagsRequest request,
        bool forceDelete,
        string userId,
        CancellationToken cancellationToken = default);
}
using System.Text.RegularExpressions;
using Neura.Core.Contracts.Tags;
using Neura.Core.Enums;

namespace Neura.Services.Services;

public partial class TagService : ITagService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TagService> _logger;

    public TagService(
        ApplicationDbContext context,
        ILogger<TagService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════════
    // QUERIES
    // ══════════════════════════════════════════════════════════════

    public async Task<Result<TagListResponse>> GetAllAsync(
        TagFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tags.AsNoTracking();

        // Apply filters
        if (filters.IsActive.HasValue) query = query.Where(t => t.IsActive == filters.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var searchTerm = filters.SearchTerm.ToLower().Trim();
            query = query.Where(t =>
                t.Name.ToLower().Contains(searchTerm) ||
                (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                t.Slug.ToLower().Contains(searchTerm));
        }

        // Get counts before pagination
        var totalTags = await _context.Tags.AsNoTracking().CountAsync(cancellationToken);
        var activeTags = await _context.Tags.AsNoTracking().CountAsync(t => t.IsActive, cancellationToken);
        var inactiveTags = totalTags - activeTags;

        // Apply sorting
        query = filters.SortBy?.ToLower() switch
        {
            "name" => filters.SortDescending
                ? query.OrderByDescending(t => t.Name)
                : query.OrderBy(t => t.Name),

            "createdon" => filters.SortDescending
                ? query.OrderByDescending(t => t.CreatedOn)
                : query.OrderBy(t => t.CreatedOn),

            "coursecount" when filters.IncludeCourseCount => filters.SortDescending
                ? query.OrderByDescending(t => t.Courses.Count)
                : query.OrderBy(t => t.Courses.Count),

            _ => filters.SortDescending
                ? query.OrderByDescending(t => t.DisplayOrder)
                : query.OrderBy(t => t.DisplayOrder)
        };

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Project and paginate
        var items = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(t => new TagResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Slug = t.Slug,
                IconUrl = t.IconUrl,
                ColorHex = t.ColorHex,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive,
                CourseCount = filters.IncludeCourseCount ? t.Courses.Count(c => !c.IsDeleted) : 0,
                CreatedOn = t.CreatedOn,
                UpdatedOn = t.UpdatedOn
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<TagResponse>(
            items,
            totalCount,
            filters.PageNumber,
            filters.PageSize);

        return Result.Success(new TagListResponse
        {
            TotalTags = totalTags,
            ActiveTags = activeTags,
            InactiveTags = inactiveTags,
            Tags = paginatedList
        });
    }

    public async Task<Result<IEnumerable<TagSummaryResponse>>> GetActiveTagsAsync(
        CancellationToken cancellationToken = default)
    {
        var tags = await _context.Tags
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .Select(t => new TagSummaryResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                IconUrl = t.IconUrl,
                ColorHex = t.ColorHex
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<TagSummaryResponse>>(tags);
    }

    public async Task<Result<TagResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TagResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Slug = t.Slug,
                IconUrl = t.IconUrl,
                ColorHex = t.ColorHex,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive,
                CourseCount = t.Courses.Count(c => !c.IsDeleted),
                CreatedOn = t.CreatedOn,
                UpdatedOn = t.UpdatedOn
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (tag is null)
            return Result.Failure<TagResponse>(TagErrors.TagNotFound);

        return Result.Success(tag);
    }

    public async Task<Result<TagResponse>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = NormalizeSlug(slug);

        var tag = await _context.Tags
            .AsNoTracking()
            .Where(t => t.Slug == normalizedSlug)
            .Select(t => new TagResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Slug = t.Slug,
                IconUrl = t.IconUrl,
                ColorHex = t.ColorHex,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive,
                CourseCount = t.Courses.Count(c => !c.IsDeleted),
                CreatedOn = t.CreatedOn,
                UpdatedOn = t.UpdatedOn
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (tag is null)
            return Result.Failure<TagResponse>(TagErrors.TagNotFound);

        return Result.Success(tag);
    }

    public async Task<Result<IEnumerable<TagSummaryResponse>>> GetPopularTagsAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var tags = await _context.Tags
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.Courses.Count(c => !c.IsDeleted && c.Status == CourseStatus.Active))
            .Take(count)
            .Select(t => new TagSummaryResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                IconUrl = t.IconUrl,
                ColorHex = t.ColorHex
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<TagSummaryResponse>>(tags);
    }

    // ══════════════════════════════════════════════════════════════
    // COMMANDS
    // ══════════════════════════════════════════════════════════════

    public async Task<Result<TagResponse>> CreateAsync(
        CreateTagRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<TagResponse>(TagErrors.InvalidName);

        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(name)
            : NormalizeSlug(request.Slug);

        // Validate slug format
        if (!IsValidSlug(slug))
            return Result.Failure<TagResponse>(TagErrors.InvalidSlug);

        // Validate color hex if provided
        if (!string.IsNullOrWhiteSpace(request.ColorHex) && !IsValidHexColor(request.ColorHex))
            return Result.Failure<TagResponse>(TagErrors.InvalidColorHex);

        // Check for duplicates
        var existingByName = await _context.Tags
            .AsNoTracking()
            .AnyAsync(t => t.Name.ToLower() == name.ToLower(), cancellationToken);

        if (existingByName)
            return Result.Failure<TagResponse>(TagErrors.DuplicateName);

        var existingBySlug = await _context.Tags
            .AsNoTracking()
            .AnyAsync(t => t.Slug == slug, cancellationToken);

        if (existingBySlug)
            return Result.Failure<TagResponse>(TagErrors.DuplicateSlug);

        // Get next display order if not provided
        var displayOrder = request.DisplayOrder;
        if (displayOrder == 0)
        {
            displayOrder = await _context.Tags
                .AsNoTracking()
                .MaxAsync(t => (int?)t.DisplayOrder, cancellationToken) ?? 0;
            displayOrder++;
        }

        // Create tag
        var tag = new Tag
        {
            Name = name,
            Description = request.Description?.Trim(),
            Slug = slug,
            IconUrl = request.IconUrl?.Trim(),
            ColorHex = NormalizeHexColor(request.ColorHex),
            DisplayOrder = displayOrder,
            IsActive = request.IsActive,
            CreatedOn = DateTime.UtcNow,
            CreatedById = userId
        };

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tag {TagId} '{TagName}' created by user {UserId}",
            tag.Id,
            tag.Name,
            userId);

        return await GetByIdAsync(tag.Id, cancellationToken);
    }

    public async Task<Result<TagResponse>> UpdateAsync(
        int id,
        UpdateTagRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tag is null)
            return Result.Failure<TagResponse>(TagErrors.TagNotFound);

        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<TagResponse>(TagErrors.InvalidName);

        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(name)
            : NormalizeSlug(request.Slug);

        if (!IsValidSlug(slug))
            return Result.Failure<TagResponse>(TagErrors.InvalidSlug);

        if (!string.IsNullOrWhiteSpace(request.ColorHex) && !IsValidHexColor(request.ColorHex))
            return Result.Failure<TagResponse>(TagErrors.InvalidColorHex);

        // Check for duplicates (excluding current tag)
        var existingByName = await _context.Tags
            .AsNoTracking()
            .AnyAsync(t => t.Id != id && t.Name.ToLower() == name.ToLower(), cancellationToken);

        if (existingByName)
            return Result.Failure<TagResponse>(TagErrors.DuplicateName);

        var existingBySlug = await _context.Tags
            .AsNoTracking()
            .AnyAsync(t => t.Id != id && t.Slug == slug, cancellationToken);

        if (existingBySlug)
            return Result.Failure<TagResponse>(TagErrors.DuplicateSlug);

        // Update
        tag.Name = name;
        tag.Description = request.Description?.Trim();
        tag.Slug = slug;
        tag.IconUrl = request.IconUrl?.Trim();
        tag.ColorHex = NormalizeHexColor(request.ColorHex);
        tag.DisplayOrder = request.DisplayOrder;
        tag.IsActive = request.IsActive;
        tag.UpdatedOn = DateTime.UtcNow;
        tag.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tag {TagId} '{TagName}' updated by user {UserId}",
            tag.Id,
            tag.Name,
            userId);

        return await GetByIdAsync(tag.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        int id,
        bool forceDelete,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags
            .Include(t => t.Courses)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tag is null)
            return Result.Failure(TagErrors.TagNotFound);

        // Check if tag is assigned to courses
        var courseCount = tag.Courses.Count(c => !c.IsDeleted);

        if (courseCount > 0 && !forceDelete)
            return Result.Failure(TagErrors.CannotDeleteTagWithCourses);

        // If force delete, remove tag from all courses first
        if (forceDelete && courseCount > 0) tag.Courses.Clear();

        // Soft delete
        tag.IsDeleted = true;
        tag.IsActive = false;
        tag.UpdatedOn = DateTime.UtcNow;
        tag.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tag {TagId} '{TagName}' deleted by user {UserId}. Force: {ForceDelete}, Courses affected: {CourseCount}",
            tag.Id,
            tag.Name,
            userId,
            forceDelete,
            courseCount);

        return Result.Success();
    }

    public async Task<Result<TagResponse>> ToggleActiveAsync(
        int id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tag is null)
            return Result.Failure<TagResponse>(TagErrors.TagNotFound);

        tag.IsActive = !tag.IsActive;
        tag.UpdatedOn = DateTime.UtcNow;
        tag.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tag {TagId} '{TagName}' active status toggled to {IsActive} by user {UserId}",
            tag.Id,
            tag.Name,
            tag.IsActive,
            userId);

        return await GetByIdAsync(tag.Id, cancellationToken);
    }

    // ══════════════════════════════════════════════════════════════
    // BULK OPERATIONS
    // ══════════════════════════════════════════════════════════════

    public async Task<Result> BulkUpdateOrderAsync(
        BulkUpdateTagsOrderRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (request.Tags.Count == 0)
            return Result.Success();

        var tagIds = request.Tags.Select(t => t.Id).ToList();

        var tags = await _context.Tags
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count != tagIds.Count)
            return Result.Failure(TagErrors.TagsNotFound);

        var orderMap = request.Tags.ToDictionary(t => t.Id, t => t.DisplayOrder);

        foreach (var tag in tags)
        {
            tag.DisplayOrder = orderMap[tag.Id];
            tag.UpdatedOn = DateTime.UtcNow;
            tag.UpdatedById = userId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bulk updated display order for {Count} tags by user {UserId}",
            tags.Count,
            userId);

        return Result.Success();
    }

    public async Task<Result> BulkToggleActiveAsync(
        BulkToggleTagsActiveRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (request.TagIds.Count == 0)
            return Result.Success();

        var tags = await _context.Tags
            .Where(t => request.TagIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count != request.TagIds.Count)
            return Result.Failure(TagErrors.TagsNotFound);

        foreach (var tag in tags)
        {
            tag.IsActive = request.IsActive;
            tag.UpdatedOn = DateTime.UtcNow;
            tag.UpdatedById = userId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bulk updated active status to {IsActive} for {Count} tags by user {UserId}",
            request.IsActive,
            tags.Count,
            userId);

        return Result.Success();
    }

    public async Task<Result> BulkDeleteAsync(
        BulkDeleteTagsRequest request,
        bool forceDelete,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (request.TagIds.Count == 0)
            return Result.Success();

        var tags = await _context.Tags
            .Include(t => t.Courses)
            .Where(t => request.TagIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count != request.TagIds.Count)
            return Result.Failure(TagErrors.TagsNotFound);

        // Check for tags with courses if not force delete
        if (!forceDelete)
        {
            var tagsWithCourses = tags.Where(t => t.Courses.Any(c => !c.IsDeleted)).ToList();
            if (tagsWithCourses.Count > 0)
                return Result.Failure(TagErrors.CannotDeleteTagWithCourses);
        }

        foreach (var tag in tags)
        {
            if (forceDelete) tag.Courses.Clear();

            tag.IsDeleted = true;
            tag.IsActive = false;
            tag.UpdatedOn = DateTime.UtcNow;
            tag.UpdatedById = userId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bulk deleted {Count} tags by user {UserId}. Force: {ForceDelete}",
            tags.Count,
            userId,
            forceDelete);

        return Result.Success();
    }

    // ══════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════

    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Convert to lowercase
        var slug = name.ToLowerInvariant();

        // Replace spaces with hyphens
        slug = slug.Replace(' ', '-');

        // Remove invalid characters (keep only letters, numbers, hyphens)
        slug = SlugRegex().Replace(slug, "");

        // Remove multiple consecutive hyphens
        slug = MultipleHyphensRegex().Replace(slug, "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return string.Empty;

        return slug.ToLowerInvariant().Trim();
    }

    private static bool IsValidSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        // Only lowercase letters, numbers, and hyphens
        return ValidSlugRegex().IsMatch(slug);
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return true; // Empty is valid (optional field)

        return HexColorRegex().IsMatch(color);
    }

    private static string? NormalizeHexColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null;

        // Ensure uppercase and # prefix
        color = color.Trim().ToUpperInvariant();

        if (!color.StartsWith('#'))
            color = "#" + color;

        return color;
    }

    // Regex patterns (source generated for performance)
    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphensRegex();

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex ValidSlugRegex();

    [GeneratedRegex(@"^#?[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
namespace Neura.Api.Infrastructure;

/// <summary>
///     Centralized cache key constants for HybridCache.
///     Keeps keys consistent across query handlers and mutation invalidation.
/// </summary>
public static class CacheKeys
{
    // ── Tags ──────────────────────────────────────────────────────────
    public const string TagsActive = "tags-active";
    public const string TagsPopularPrefix = "tags-popular";

    /// <summary>Generates a cache key for popular tags by count, e.g. "tags-popular-10".</summary>
    public static string TagsPopular(int count) => $"{TagsPopularPrefix}-{count}";

    /// <summary>All tag-related cache keys that should be invalidated on tag mutations.</summary>
    public static readonly string[] AllTagKeys =
    [
        TagsActive,
        TagsPopular(5),
        TagsPopular(10),
        TagsPopular(15),
        TagsPopular(20)
    ];

    // ── Courses ───────────────────────────────────────────────────────
    public const string CourseFullContent = "course-full-content";
    public const string CourseStatsPrefix = "course-stats";

    /// <summary>Generates a cache key for batched course stats, e.g. "course-stats-1,2,3".</summary>
    public static string CourseStats(IEnumerable<int> courseIds) =>
        $"{CourseStatsPrefix}-{string.Join(",", courseIds.OrderBy(id => id))}";
}

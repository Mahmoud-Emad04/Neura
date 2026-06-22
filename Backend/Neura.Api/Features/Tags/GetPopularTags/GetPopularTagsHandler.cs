using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Tags;
using Neura.Core.Enums;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.GetPopularTags;

internal sealed class GetPopularTagsHandler(
    ApplicationDbContext context,
    HybridCache hybridCache)
    : IRequestHandler<GetPopularTagsQuery, Result<IEnumerable<TagSummaryResponse>>>
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    public async Task<Result<IEnumerable<TagSummaryResponse>>> Handle(
        GetPopularTagsQuery query, CancellationToken ct)
    {
        var tags = await hybridCache.GetOrCreateAsync(
            CacheKeys.TagsPopular(query.Count),
            async cancel => await context.Tags
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.Courses.Count(c => !c.IsDeleted && c.Status == CourseStatus.Active))
                .Take(query.Count)
                .Select(t => new TagSummaryResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    IconUrl = t.IconUrl,
                    ColorHex = t.ColorHex
                })
                .ToListAsync(cancel),
            CacheOptions,
            cancellationToken: ct);

        return Result.Success<IEnumerable<TagSummaryResponse>>(tags);
    }
}

using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Tags;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.GetActiveTags;

internal sealed class GetActiveTagsHandler(
    ApplicationDbContext context,
    HybridCache hybridCache)
    : IRequestHandler<GetActiveTagsQuery, Result<IEnumerable<TagSummaryResponse>>>
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    public async Task<Result<IEnumerable<TagSummaryResponse>>> Handle(
        GetActiveTagsQuery query, CancellationToken ct)
    {
        var tags = await hybridCache.GetOrCreateAsync(
            CacheKeys.TagsActive,
            async cancel => await context.Tags
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
                .ToListAsync(cancel),
            CacheOptions,
            cancellationToken: ct);

        return Result.Success<IEnumerable<TagSummaryResponse>>(tags);
    }
}

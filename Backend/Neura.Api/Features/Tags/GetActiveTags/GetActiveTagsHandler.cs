using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.GetActiveTags;

internal sealed class GetActiveTagsHandler(ApplicationDbContext context) 
    : IRequestHandler<GetActiveTagsQuery, Result<IEnumerable<TagSummaryResponse>>>
{
    public async Task<Result<IEnumerable<TagSummaryResponse>>> Handle(
        GetActiveTagsQuery query, CancellationToken ct)
    {
        var tags = await context.Tags
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
            .ToListAsync(ct);

        return Result.Success<IEnumerable<TagSummaryResponse>>(tags);
    }
}

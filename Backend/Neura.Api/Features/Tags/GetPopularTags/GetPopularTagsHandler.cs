using MediatR;
using Neura.Core.Contracts.Tags;
using Neura.Core.Enums;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.GetPopularTags;

internal sealed class GetPopularTagsHandler(ApplicationDbContext context)
    : IRequestHandler<GetPopularTagsQuery, Result<IEnumerable<TagSummaryResponse>>>
{
    public async Task<Result<IEnumerable<TagSummaryResponse>>> Handle(
        GetPopularTagsQuery query, CancellationToken ct)
    {
        var tags = await context.Tags
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
            .ToListAsync(ct);

        return Result.Success<IEnumerable<TagSummaryResponse>>(tags);
    }
}

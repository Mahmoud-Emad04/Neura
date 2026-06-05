using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.GetTagBySlug;

internal sealed class GetTagBySlugHandler(ApplicationDbContext context) 
    : IRequestHandler<GetTagBySlugQuery, Result<TagResponse>>
{
    public async Task<Result<TagResponse>> Handle(
        GetTagBySlugQuery query, CancellationToken ct)
    {
        var normalizedSlug = TagHelpers.NormalizeSlug(query.Slug);

        var tag = await context.Tags
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
            .SingleOrDefaultAsync(ct);

        if (tag is null)
            return Result.Failure<TagResponse>(TagErrors.TagNotFound);

        return Result.Success(tag);
    }
}

using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Tags;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.ToggleTagActive;

internal sealed class ToggleTagActiveHandler(ApplicationDbContext context, HybridCache hybridCache)
    : IRequestHandler<ToggleTagActiveCommand, Result<TagResponse>>
{
    public async Task<Result<TagResponse>> Handle(
        ToggleTagActiveCommand command, CancellationToken ct)
    {
        var tag = await context.Tags
            .Include(t => t.Courses)
            .SingleOrDefaultAsync(t => t.Id == command.Id, ct);

        if (tag is null)
            return Result.Failure<TagResponse>(TagErrors.TagNotFound);

        tag.IsActive = !tag.IsActive;
        tag.UpdatedOn = DateTime.UtcNow;
        tag.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        // Invalidate tag caches
        foreach (var key in CacheKeys.AllTagKeys)
            await hybridCache.RemoveAsync(key, ct);

        return Result.Success(new TagResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            Slug = tag.Slug,
            IconUrl = tag.IconUrl,
            ColorHex = tag.ColorHex,
            DisplayOrder = tag.DisplayOrder,
            IsActive = tag.IsActive,
            CourseCount = tag.Courses.Count(c => !c.IsDeleted),
            CreatedOn = tag.CreatedOn,
            UpdatedOn = tag.UpdatedOn
        });
    }
}

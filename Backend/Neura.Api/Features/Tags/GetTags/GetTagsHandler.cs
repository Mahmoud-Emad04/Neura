using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.GetTags;

internal sealed class GetTagsHandler(ApplicationDbContext context) 
    : IRequestHandler<GetTagsQuery, Result<TagListResponse>>
{
    public async Task<Result<TagListResponse>> Handle(
        GetTagsQuery query, CancellationToken ct)
    {
        var filters = query.Filters;
        var queryable = context.Tags.AsNoTracking();

        if (filters.IsActive.HasValue) 
            queryable = queryable.Where(t => t.IsActive == filters.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var searchTerm = filters.SearchTerm.ToLower().Trim();
            queryable = queryable.Where(t =>
                t.Name.ToLower().Contains(searchTerm) ||
                (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                t.Slug.ToLower().Contains(searchTerm));
        }

        var totalTags = await context.Tags.AsNoTracking().CountAsync(ct);
        var activeTags = await context.Tags.AsNoTracking().CountAsync(t => t.IsActive, ct);
        var inactiveTags = totalTags - activeTags;

        queryable = filters.SortBy?.ToLower() switch
        {
            "name" => filters.SortDescending
                ? queryable.OrderByDescending(t => t.Name)
                : queryable.OrderBy(t => t.Name),

            "createdon" => filters.SortDescending
                ? queryable.OrderByDescending(t => t.CreatedOn)
                : queryable.OrderBy(t => t.CreatedOn),

            "coursecount" when filters.IncludeCourseCount => filters.SortDescending
                ? queryable.OrderByDescending(t => t.Courses.Count)
                : queryable.OrderBy(t => t.Courses.Count),

            _ => filters.SortDescending
                ? queryable.OrderByDescending(t => t.DisplayOrder)
                : queryable.OrderBy(t => t.DisplayOrder)
        };

        var totalCount = await queryable.CountAsync(ct);

        var items = await queryable
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
            .ToListAsync(ct);

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
}

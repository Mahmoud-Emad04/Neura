using MediatR;
using Neura.Core.Contracts.Announcement;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.GetAllPosts;

internal sealed class GetAllPostsHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetAllPostsQuery, Result<PaginatedList<PostResponse>>>
{
    public async Task<Result<PaginatedList<PostResponse>>> Handle(
        GetAllPostsQuery query, CancellationToken ct)
    {
        var baseUrl = helpers.GetBaseUrl();

        var baseQuery = context.Posts
            .AsNoTracking()
            .Where(p => p.IsPublic && !p.IsDeleted);

        var totalCount = await baseQuery.CountAsync(ct);

        var projections = await AnnouncementHelpers.ProjectPosts(baseQuery, query.CurrentUserId)
            .OrderByDescending(p => p.CreatedOn)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .AsSplitQuery()
            .ToListAsync(ct);

        var mapped = projections.Select(p => AnnouncementHelpers.MapProjectionToResponse(p, baseUrl, query.CurrentUserId)).ToList();
        var result = new PaginatedList<PostResponse>(mapped, query.PageNumber, totalCount, query.PageSize);

        return Result.Success(result);
    }
}

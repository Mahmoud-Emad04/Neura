using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.GetCurrentUserPosts;

internal sealed class GetCurrentUserPostsHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers) 
    : IRequestHandler<GetCurrentUserPostsQuery, Result<PaginatedList<PostResponse>>>
{
    public async Task<Result<PaginatedList<PostResponse>>> Handle(
        GetCurrentUserPostsQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
            return Result.Failure<PaginatedList<PostResponse>>(AnnouncementErrors.PostAccessDenied);

        var baseUrl = helpers.GetBaseUrl();

        var baseQuery = context.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.CreatedById == query.UserId);

        if (query.IsPublic.HasValue)
            baseQuery = baseQuery.Where(p => p.IsPublic == query.IsPublic.Value);

        var totalCount = await baseQuery.CountAsync(ct);

        var projections = await AnnouncementHelpers.ProjectPosts(baseQuery, query.UserId)
            .OrderByDescending(p => p.CreatedOn)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .AsSplitQuery()
            .ToListAsync(ct);

        var mapped = projections.Select(p => AnnouncementHelpers.MapProjectionToResponse(p, baseUrl, query.UserId)).ToList();
        var result = new PaginatedList<PostResponse>(mapped, query.PageNumber, totalCount, query.PageSize);

        return Result.Success(result);
    }
}

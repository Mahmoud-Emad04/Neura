using MediatR;
using Neura.Core.Contracts.Announcement;

namespace Neura.Api.Features.Announcements.GetCurrentUserPosts;

public sealed record GetCurrentUserPostsQuery(string UserId, bool? IsPublic = null, int PageNumber = 1, int PageSize = 10)
    : IRequest<Result<PaginatedList<PostResponse>>>;

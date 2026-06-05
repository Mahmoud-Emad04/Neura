using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Announcement;

namespace Neura.Api.Features.Announcements.GetAllPosts;

public sealed record GetAllPostsQuery(int PageNumber = 1, int PageSize = 10, string? CurrentUserId = null) 
    : IRequest<Result<PaginatedList<PostResponse>>>;

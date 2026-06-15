using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Announcements.GetPostById;

internal sealed class GetPostByIdHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetPostByIdQuery, Result<PostResponse>>
{
    public async Task<Result<PostResponse>> Handle(
        GetPostByIdQuery query, CancellationToken ct)
    {
        var baseUrl = helpers.GetBaseUrl();

        var projection = await AnnouncementHelpers.ProjectPosts(
                context.Posts.AsNoTracking().Where(p => !p.IsDeleted && p.Id == query.PostId).AsSplitQuery(),
                query.CurrentUserId)
            .FirstOrDefaultAsync(ct);

        if (projection is null)
            return Result.Failure<PostResponse>(AnnouncementErrors.PostNotFound);

        var isAdmin = helpers.IsUserInRole(DefaultRoles.Admin) || helpers.IsUserInRole(DefaultRoles.SuperAdmin);

        if (!projection.IsPublic && projection.CreatedById != query.CurrentUserId && !isAdmin)
            return Result.Failure<PostResponse>(AnnouncementErrors.PostAccessDenied);

        return Result.Success(AnnouncementHelpers.MapProjectionToResponse(projection, baseUrl, query.CurrentUserId));
    }
}

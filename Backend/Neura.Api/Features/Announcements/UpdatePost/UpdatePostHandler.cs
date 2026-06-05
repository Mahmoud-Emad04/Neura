using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;
using Neura.Api.Features.Announcements.GetPostById;

namespace Neura.Api.Features.Announcements.UpdatePost;

internal sealed class UpdatePostHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers,
    ISender sender) 
    : IRequestHandler<UpdatePostCommand, Result<PostResponse>>
{
    public async Task<Result<PostResponse>> Handle(
        UpdatePostCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostResponse>(AnnouncementErrors.PostInvalidData);

        var post = await context.Posts
            .FirstOrDefaultAsync(p => p.Id == command.PostId && !p.IsDeleted, ct);

        if (post is null)
            return Result.Failure<PostResponse>(AnnouncementErrors.PostNotFound);

        var isAdmin = helpers.IsUserInRole(DefaultRoles.Admin) || helpers.IsUserInRole(DefaultRoles.SuperAdmin);

        if (post.CreatedById != command.UserId && !isAdmin)
            return Result.Failure<PostResponse>(AnnouncementErrors.PostAccessDenied);

        post.Title = request.Title;
        post.Content = request.Content;
        post.IsPublic = request.IsPublic;
        post.UpdatedOn = DateTime.UtcNow;
        post.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        return await sender.Send(new GetPostByIdQuery(post.Id, command.UserId), ct);
    }
}

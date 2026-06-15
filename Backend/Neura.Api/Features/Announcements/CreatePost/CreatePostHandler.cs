using MediatR;
using Neura.Api.Features.Announcements.GetPostById;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Errors;
using Neura.Core.FilesConsts;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Announcements.CreatePost;

internal sealed class CreatePostHandler(
    ApplicationDbContext context,
    ISender sender,
    IFileService fileService)
    : IRequestHandler<CreatePostCommand, Result<PostResponse>>
{
    public async Task<Result<PostResponse>> Handle(
        CreatePostCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostResponse>(AnnouncementErrors.PostInvalidData);

        if (request.CourseId.HasValue)
        {
            var courseExists = await context.Courses
                .AnyAsync(c => c.Id == request.CourseId.Value && !c.IsDeleted, ct);

            if (!courseExists)
                return Result.Failure<PostResponse>(AnnouncementErrors.CourseNotFound);
        }

        if (request.SectionId.HasValue)
        {
            var sectionExists = await context.Sections
                .AnyAsync(s => s.Id == request.SectionId.Value && !s.IsDeleted, ct);

            if (!sectionExists)
                return Result.Failure<PostResponse>(AnnouncementErrors.SectionNotFound);
        }

        var post = new Post
        {
            Title = request.Title,
            Content = request.Content,
            IsPublic = request.IsPublic,
            CourseId = request.CourseId,
            SectionId = request.SectionId,
            CreatedById = command.UserId,
            CreatedOn = DateTime.UtcNow
        };

        if (request.Image is not null)
            post.ImageUrl = await fileService.UploadImageAsync(request.Image, ImageConsts.Post, ct);

        context.Posts.Add(post);
        await context.SaveChangesAsync(ct);

        // Fetch back through GetPostByIdQuery to preserve standard projection and formatting
        return await sender.Send(new GetPostByIdQuery(post.Id, command.UserId), ct);
    }
}

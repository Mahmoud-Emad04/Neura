using Neura.Core.Contracts.Announcement;
using Neura.Core.Contracts.Files;
using Neura.Core.FilesConsts;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class AnnouncementService(
    ApplicationDbContext context,
    IServiceHelpers helpers,
    IFileService fileService,
    ILogger<AnnouncementService> logger) : IAnnouncementService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IFileService _fileService = fileService;
    private readonly IServiceHelpers _helpers = helpers;
    private readonly ILogger<AnnouncementService> _logger = logger;

    public async Task<Result<PaginatedList<PostResponse>>> GetAllPostsAsync(int pageNumber = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserId();

        var postsQuery = _context.Posts
            .Where(p => p.IsPublic && !p.IsDeleted)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeleted && c.ParentCommentId == null))
            .ThenInclude(c => c.Replies.Where(r => !r.IsDeleted))
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedOn);

        var paginatedResponse = await PaginatedList<PostResponse>.CreateAsync(
            postsQuery.Select(p => MapPostToResponse(p, currentUserId, _helpers.GetBaseUrl())),
            pageNumber,
            pageSize,
            cancellationToken: cancellationToken);

        return Result.Success(paginatedResponse);
    }

    public async Task<Result<PaginatedList<PostResponse>>> GetCurrentUserPostsAsync(bool? isPublic = null,
        int pageNumber = 1,
        int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Result.Failure<PaginatedList<PostResponse>>(AnnouncementErrors.PostAccessDenied);

        var postsQuery = _context.Posts
            .Where(p => !p.IsDeleted && p.CreatedById == currentUserId)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeleted && c.ParentCommentId == null))
            .ThenInclude(c => c.Replies.Where(r => !r.IsDeleted))
            .AsNoTracking()
            .AsQueryable();

        if (isPublic.HasValue)
            postsQuery = postsQuery.Where(p => p.IsPublic == isPublic.Value);

        postsQuery = postsQuery.OrderByDescending(p => p.CreatedOn);

        var paginatedResponse = await PaginatedList<PostResponse>.CreateAsync(
            postsQuery.Select(p => MapPostToResponse(p, currentUserId, _helpers.GetBaseUrl())),
            pageNumber,
            pageSize,
            cancellationToken: cancellationToken);

        return Result.Success(paginatedResponse);
    }

    public async Task<Result<PostResponse>> GetPostByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserId();

        var post = await _context.Posts
            .Where(p => !p.IsDeleted)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeleted && c.ParentCommentId == null))
            .ThenInclude(c => c.Replies.Where(r => !r.IsDeleted))
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (post is null)
            return Result.Failure<PostResponse>(AnnouncementErrors.PostNotFound);

        // Check visibility: user can view private posts if they're the creator or admin
        if (!post.IsPublic && post.CreatedById != currentUserId && !IsAdmin())
            return Result.Failure<PostResponse>(AnnouncementErrors.PostAccessDenied);

        var response = MapPostToResponse(post, currentUserId, _helpers.GetBaseUrl());

        return Result.Success(response);
    }

    public async Task<Result<PostResponse>> CreatePostAsync(PostRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        // Validate post data
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<PostResponse>(AnnouncementErrors.PostInvalidData);

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostResponse>(AnnouncementErrors.PostInvalidData);

        // Validate course if provided
        if (request.CourseId.HasValue)
        {
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == request.CourseId.Value && !c.IsDeleted, cancellationToken);

            if (!courseExists)
                return Result.Failure<PostResponse>(AnnouncementErrors.CourseNotFound);
        }

        // Validate section if provided
        if (request.SectionId.HasValue)
        {
            var sectionExists = await _context.Sections
                .AnyAsync(s => s.Id == request.SectionId.Value && !s.IsDeleted, cancellationToken);

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
            CreatedById = userId,
            CreatedOn = DateTime.UtcNow
        };

        // Upload image if provided
        if (request.Image is not null)
            post.ImageUrl = await _fileService.UploadImageAsync(request.Image, ImageConsts.Post, cancellationToken);

        _context.Posts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        var response = MapPostToResponse(post, userId, _helpers.GetBaseUrl());

        return Result.Success(response);
    }

    public async Task<Result> RemovePostAsync(int postId, string userId, CancellationToken cancellationToken = default)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        // Check permissions: only creator or admin can delete
        if (post.CreatedById != userId && !IsAdmin())
            return Result.Failure(AnnouncementErrors.PostAccessDenied);

        post.IsDeleted = true;
        post.DeletedOn = DateTime.UtcNow;
        post.DeletedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PostResponse>> UpdatePostAsync(int postId, PostUpdateRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var post = await _context.Posts
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeleted && c.ParentCommentId == null))
            .ThenInclude(c => c.Replies.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure<PostResponse>(AnnouncementErrors.PostNotFound);

        // Check permissions: only creator or admin can update
        if (post.CreatedById != userId && !IsAdmin())
            return Result.Failure<PostResponse>(AnnouncementErrors.PostAccessDenied);

        // Validate update data
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<PostResponse>(AnnouncementErrors.PostInvalidData);

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostResponse>(AnnouncementErrors.PostInvalidData);

        post.Title = request.Title;
        post.Content = request.Content;
        post.IsPublic = request.IsPublic;
        post.UpdatedOn = DateTime.UtcNow;
        post.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var response = MapPostToResponse(post, userId, _helpers.GetBaseUrl());

        return Result.Success(response);
    }

    public async Task<Result> UpdatePostImageAsync(int postId, UploadImageRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        // Check permissions: only creator or admin can update
        if (post.CreatedById != userId && !IsAdmin())
            return Result.Failure(AnnouncementErrors.PostAccessDenied);

        // Delete old image if exists
        if (!string.IsNullOrEmpty(post.ImageUrl))
            _fileService.Delete(post.ImageUrl);

        post.ImageUrl = await _fileService.UploadImageAsync(request.Image, ImageConsts.Post, cancellationToken);
        post.UpdatedOn = DateTime.UtcNow;
        post.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PostCommentResponse>> AddPostCommentAsync(int postId, PostCommentRequest request,
        string userId, CancellationToken cancellationToken = default)
    {
        // Validate post exists
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.PostNotFound);

        // Validate comment data
        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentInvalidData);

        // Validate parent comment if provided
        if (request.ParentCommentId.HasValue)
        {
            var parentComment = await _context.PostComments
                .FirstOrDefaultAsync(c => c.Id == request.ParentCommentId.Value && !c.IsDeleted && c.PostId == postId,
                    cancellationToken);

            if (parentComment is null)
                return Result.Failure<PostCommentResponse>(AnnouncementErrors.ParentCommentNotFound);
        }

        var comment = new PostComment
        {
            PostId = postId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content,
            CreatedById = userId,
            CreatedOn = DateTime.UtcNow
        };

        // Upload image if provided
        if (request.Image is not null)
            comment.ImageUrl =
                await _fileService.UploadImageAsync(request.Image, ImageConsts.PostComment, cancellationToken);

        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        var response = MapCommentToResponse(comment, new List<PostComment>());

        return Result.Success(response);
    }

    public async Task<Result> RemovePostCommentAsync(int commentId, string userId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _context.PostComments
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
            return Result.Failure(AnnouncementErrors.CommentNotFound);

        // Check permissions: only creator or admin can delete
        if (comment.CreatedById != userId && !IsAdmin())
            return Result.Failure(AnnouncementErrors.CommentAccessDenied);

        comment.IsDeleted = true;
        comment.DeletedOn = DateTime.UtcNow;
        comment.DeletedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PostCommentResponse>> UpdatePostCommentAsync(int commentId,
        PostCommentUpdateRequest request, string userId, CancellationToken cancellationToken = default)
    {
        var comment = await _context.PostComments
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, cancellationToken);

        if (comment is null)
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentNotFound);

        // Check permissions: only creator or admin can update
        if (comment.CreatedById != userId && !IsAdmin())
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentAccessDenied);

        // Validate update data
        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentInvalidData);

        comment.Content = request.Content;
        comment.UpdatedOn = DateTime.UtcNow;
        comment.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var response = MapCommentToResponse(comment, comment.Replies.ToList());

        return Result.Success(response);
    }

    public async Task<Result> UpdatePostCommentImageAsync(int commentId, UploadImageRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _context.PostComments
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, cancellationToken);

        if (comment is null)
            return Result.Failure(AnnouncementErrors.CommentNotFound);

        // Check permissions: only creator or admin can update
        if (comment.CreatedById != userId && !IsAdmin())
            return Result.Failure(AnnouncementErrors.CommentAccessDenied);

        // Delete old image if exists
        if (!string.IsNullOrEmpty(comment.ImageUrl))
            _fileService.Delete(comment.ImageUrl);

        comment.ImageUrl =
            await _fileService.UploadImageAsync(request.Image, ImageConsts.PostComment, cancellationToken);
        comment.UpdatedOn = DateTime.UtcNow;
        comment.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> TogglePostLikeAsync(int postId, string userId,
        CancellationToken cancellationToken = default)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        var existingLike = await _context.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken);

        if (existingLike is not null)
        {
            _context.PostLikes.Remove(existingLike);
        }
        else
        {
            var like = new PostLike
            {
                PostId = postId,
                UserId = userId,
                CreatedOn = DateTime.UtcNow
            };

            _context.PostLikes.Add(like);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> TogglePostVisibilityAsync(int postId, string userId,
        CancellationToken cancellationToken = default)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        // Check permissions: only creator or admin can change visibility
        if (post.CreatedById != userId && !IsAdmin())
            return Result.Failure(AnnouncementErrors.PostAccessDenied);

        post.IsPublic = !post.IsPublic;
        post.UpdatedOn = DateTime.UtcNow;
        post.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private string? CurrentUserId()
    {
        return _helpers.GetCurrentUserId();
    }

    private bool IsAdmin()
    {
        return _helpers.IsUserInRole("Admin");
    }

    #region Private Helpers

    private static PostResponse MapPostToResponse(Post post, string? currentUserId, string baseUrl)
    {
        var isLikedByCurrentUser =
            string.IsNullOrEmpty(currentUserId) ? false : post.Likes.Any(l => l.UserId == currentUserId);

        var comments = post.Comments
            .Where(c => c.ParentCommentId == null)
            .Select(c => MapCommentToResponse(c, c.Replies.ToList()))
            .ToList();
        return new PostResponse(
            post.Id,
            post.Title,
            post.Content,
            post.IsPublic,
            post.CourseId,
            post.SectionId,
            post.ImageUrl is null ? post.ImageUrl : $"{baseUrl}/{post.ImageUrl}",
            post.Likes.Count,
            post.CreatedOn,
            post.UpdatedOn,
            post.CreatedById,
            post.UpdatedById,
            isLikedByCurrentUser,
            comments
        );
    }

    private static PostCommentResponse MapCommentToResponse(PostComment comment, List<PostComment> replies)
    {
        var mappedReplies = replies
            .Select(r => MapCommentToResponse(r, r.Replies.ToList()))
            .ToList();

        return new PostCommentResponse(
            comment.Id,
            comment.PostId,
            comment.ParentCommentId,
            comment.Content,
            comment.ImageUrl,
            comment.CreatedOn,
            comment.UpdatedOn,
            comment.CreatedById,
            comment.UpdatedById,
            mappedReplies
        );
    }

    #endregion
}
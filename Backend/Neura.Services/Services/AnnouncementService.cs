using Neura.Core.Abstractions.Consts;
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

    // ---------------------------------------------------------------------
    // Read endpoints
    // ---------------------------------------------------------------------

    public async Task<Result<PaginatedList<PostResponse>>> GetAllPostsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserId();
        var baseUrl = _helpers.GetBaseUrl();

        var baseQuery = _context.Posts
            .AsNoTracking()
            .Where(p => p.IsPublic && !p.IsDeleted);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var projections = await ProjectPosts(baseQuery, currentUserId)
            .OrderByDescending(p => p.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

		var mapped = projections.Select(p => MapProjectionToResponse(p, baseUrl, currentUserId)).ToList();
		var result = new PaginatedList<PostResponse>(mapped, pageNumber, totalCount, pageSize);

        return Result.Success(result);
    }

    public async Task<Result<PaginatedList<PostResponse>>> GetCurrentUserPostsAsync(
        bool? isPublic = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
            return Result.Failure<PaginatedList<PostResponse>>(AnnouncementErrors.PostAccessDenied);

        var baseUrl = _helpers.GetBaseUrl();

        var baseQuery = _context.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.CreatedById == currentUserId);

        if (isPublic.HasValue)
            baseQuery = baseQuery.Where(p => p.IsPublic == isPublic.Value);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var projections = await ProjectPosts(baseQuery, currentUserId)
            .OrderByDescending(p => p.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

		var mapped = projections.Select(p => MapProjectionToResponse(p, baseUrl, currentUserId)).ToList();
		var result = new PaginatedList<PostResponse>(mapped, pageNumber, totalCount, pageSize);

        return Result.Success(result);
    }

    public async Task<Result<PostResponse>> GetPostByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var currentUserId = CurrentUserId();
        var baseUrl = _helpers.GetBaseUrl();

        var projection = await ProjectPosts(
                _context.Posts.AsNoTracking().Where(p => !p.IsDeleted && p.Id == id).AsSplitQuery(),
                currentUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (projection is null)
            return Result.Failure<PostResponse>(AnnouncementErrors.PostNotFound);

        // Visibility: only creator or admin can see private posts
        if (!projection.IsPublic && projection.CreatedById != currentUserId && !IsAdmin())
            return Result.Failure<PostResponse>(AnnouncementErrors.PostAccessDenied);

		return Result.Success(MapProjectionToResponse(projection, baseUrl, currentUserId));
	}

    // ---------------------------------------------------------------------
    // Write endpoints — posts
    // ---------------------------------------------------------------------

    public async Task<Result<PostResponse>> CreatePostAsync(
        PostRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostResponse>(AnnouncementErrors.PostInvalidData);

        if (request.CourseId.HasValue)
        {
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == request.CourseId.Value && !c.IsDeleted, cancellationToken);

            if (!courseExists)
                return Result.Failure<PostResponse>(AnnouncementErrors.CourseNotFound);
        }

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

        if (request.Image is not null)
            post.ImageUrl = await _fileService.UploadImageAsync(request.Image, ImageConsts.Post, cancellationToken);

        _context.Posts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        // Re-query through the projection to get a consistent response shape
        return await GetPostByIdAsync(post.Id, cancellationToken);
    }

    public async Task<Result> RemovePostAsync(int postId, string userId, CancellationToken cancellationToken = default)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        if (post.CreatedById != userId && !IsAdmin())
            return Result.Failure(AnnouncementErrors.PostAccessDenied);

        post.IsDeleted = true;
        post.DeletedOn = DateTime.UtcNow;
        post.DeletedById = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PostResponse>> UpdatePostAsync(
        int postId,
        PostUpdateRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostResponse>(AnnouncementErrors.PostInvalidData);

        // Tracked load — minimal columns, no Include needed for the update itself
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure<PostResponse>(AnnouncementErrors.PostNotFound);

        if (post.CreatedById != userId && !IsAdmin())
            return Result.Failure<PostResponse>(AnnouncementErrors.PostAccessDenied);

        post.Title = request.Title;
        post.Content = request.Content;
        post.IsPublic = request.IsPublic;
        post.UpdatedOn = DateTime.UtcNow;
        post.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        // Re-query via projection for the response
        return await GetPostByIdAsync(post.Id, cancellationToken);
    }

    public async Task<Result> UpdatePostImageAsync(
        int postId,
        UploadImageRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        if (post.CreatedById != userId && !IsAdmin())
            return Result.Failure(AnnouncementErrors.PostAccessDenied);

        if (!string.IsNullOrEmpty(post.ImageUrl))
            _fileService.Delete(post.ImageUrl);

        post.ImageUrl = await _fileService.UploadImageAsync(request.Image, ImageConsts.Post, cancellationToken);
        post.UpdatedOn = DateTime.UtcNow;
        post.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ---------------------------------------------------------------------
    // Write endpoints — comments
    // ---------------------------------------------------------------------

    public async Task<Result<PostCommentResponse>> AddPostCommentAsync(
        int postId,
        PostCommentRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentInvalidData);

        var postExists = await _context.Posts
            .AnyAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (!postExists)
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.PostNotFound);

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await _context.PostComments
                .AnyAsync(c => c.Id == request.ParentCommentId.Value
                               && !c.IsDeleted
                               && c.PostId == postId, cancellationToken);

            if (!parentExists)
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

        if (request.Image is not null)
            comment.ImageUrl = await _fileService.UploadImageAsync(
                request.Image, ImageConsts.PostComment, cancellationToken);

        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        // Fetch the just-created comment as a flat projection (single fast query)
        var baseUrl = _helpers.GetBaseUrl();
        var projection = await ProjectComments(
                _context.PostComments.AsNoTracking().Where(c => c.Id == comment.Id))
            .FirstAsync(cancellationToken);

		// Brand-new comment has no replies
		var response = MapCommentProjectionToResponse(
			projection,
			EmptyRepliesLookup,
			baseUrl,
			currentUserId: userId);

        return Result.Success(response);
    }

    public async Task<Result> RemovePostCommentAsync(
        int commentId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _context.PostComments
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
            return Result.Failure(AnnouncementErrors.CommentNotFound);

        if (comment.CreatedById == userId || IsAdmin() || comment.Post.CreatedById == userId)
        {
            comment.IsDeleted = true;
            comment.DeletedOn = DateTime.UtcNow;
            comment.DeletedById = userId;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        return Result.Failure(AnnouncementErrors.CommentAccessDenied);
    }

    public async Task<Result<PostCommentResponse>> UpdatePostCommentAsync(
        int commentId,
        PostCommentUpdateRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentInvalidData);

        var comment = await _context.PostComments
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, cancellationToken);

        if (comment is null)
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentNotFound);

        if (comment.CreatedById != userId && !IsAdmin())
            return Result.Failure<PostCommentResponse>(AnnouncementErrors.CommentAccessDenied);

        comment.Content = request.Content;
        comment.UpdatedOn = DateTime.UtcNow;
        comment.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        // Re-fetch the comment + its non-deleted replies as flat projections
        var baseUrl = _helpers.GetBaseUrl();

        var commentAndReplies = await ProjectComments(
                _context.PostComments
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted && (c.Id == commentId || c.ParentCommentId == commentId)))
            .ToListAsync(cancellationToken);

		var root = commentAndReplies.First(c => c.Id == commentId);
		var repliesLookup = BuildRepliesLookup(commentAndReplies);
		var response = MapCommentProjectionToResponse(root, repliesLookup, baseUrl, currentUserId: userId);

        return Result.Success(response);
    }

    public async Task<Result> UpdatePostCommentImageAsync(
        int commentId,
        UploadImageRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _context.PostComments
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, cancellationToken);

        if (comment is null)
            return Result.Failure(AnnouncementErrors.CommentNotFound);

        if (comment.CreatedById != userId && !IsAdmin())
            return Result.Failure(AnnouncementErrors.CommentAccessDenied);

        if (!string.IsNullOrEmpty(comment.ImageUrl))
            _fileService.Delete(comment.ImageUrl);

        comment.ImageUrl = await _fileService.UploadImageAsync(
            request.Image, ImageConsts.PostComment, cancellationToken);
        comment.UpdatedOn = DateTime.UtcNow;
        comment.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ---------------------------------------------------------------------
    // Toggles
    // ---------------------------------------------------------------------

    public async Task<Result> TogglePostLikeAsync(
        int postId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var postExists = await _context.Posts
            .AnyAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (!postExists)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        var existingLike = await _context.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken);

        if (existingLike is not null)
        {
            _context.PostLikes.Remove(existingLike);
        }
        else
        {
            _context.PostLikes.Add(new PostLike
            {
                PostId = postId,
                UserId = userId,
                CreatedOn = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> TogglePostVisibilityAsync(
        int postId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure(AnnouncementErrors.PostNotFound);

        if (post.CreatedById != userId && !IsAdmin())
            return Result.Failure(AnnouncementErrors.PostAccessDenied);

        post.IsPublic = !post.IsPublic;
        post.UpdatedOn = DateTime.UtcNow;
        post.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private string? CurrentUserId() => _helpers.GetCurrentUserId();
    private bool IsAdmin() => _helpers.IsUserInRole(DefaultRoles.Admin) || _helpers.IsUserInRole(DefaultRoles.SuperAdmin);

    // ---------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------

    #region Private Helpers

    private static readonly IReadOnlyDictionary<int, List<PostCommentProjection>> EmptyRepliesLookup
        = new Dictionary<int, List<PostCommentProjection>>();

    /// <summary>
    /// Lightweight DTO for a post + its creator + computed counts. Only the columns
    /// the API actually uses are pulled from the database.
    /// </summary>
    private sealed class PostProjection
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public int? CourseId { get; set; }
        public int? SectionId { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string? UpdatedById { get; set; }
        public string? CreatorFirstName { get; set; }
        public string? CreatorLastName { get; set; }
        public string? CreatorImageUrl { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<PostCommentProjection> Comments { get; set; } = new();
    }

    /// <summary>
    /// Lightweight DTO for a comment + its creator. Used for both the embedded
    /// comments inside a post and the standalone comment endpoints.
    /// </summary>
    private sealed class PostCommentProjection
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string? UpdatedById { get; set; }
        public string? CreatorFirstName { get; set; }
        public string? CreatorLastName { get; set; }
        public string? CreatorImageUrl { get; set; }
    }

    /// <summary>
    /// Translates a <see cref="Post"/> query into a fully server-evaluated
    /// <see cref="PostProjection"/> query (counts via SQL, no Includes).
    /// </summary>
    private static IQueryable<PostProjection> ProjectPosts(IQueryable<Post> posts, string? currentUserId)
    {
        return posts.Select(p => new PostProjection
        {
            Id = p.Id,
            Title = p.Title,
            Content = p.Content,
            IsPublic = p.IsPublic,
            CourseId = p.CourseId,
            SectionId = p.SectionId,
            ImageUrl = p.ImageUrl,
            CreatedOn = p.CreatedOn,
            UpdatedOn = p.UpdatedOn,
            CreatedById = p.CreatedById,
            UpdatedById = p.UpdatedById,
            CreatorFirstName = p.CreatedBy != null ? p.CreatedBy.FirstName : null,
            CreatorLastName = p.CreatedBy != null ? p.CreatedBy.LastName : null,
            CreatorImageUrl = p.CreatedBy != null ? p.CreatedBy.ImageUrl : null,
            LikesCount = p.Likes.Count,
            CommentsCount = p.Comments.Count(c => !c.IsDeleted),
            IsLikedByCurrentUser = currentUserId != null
                                   && p.Likes.Any(l => l.UserId == currentUserId),
            Comments = p.Comments
                .Where(c => !c.IsDeleted)
                .Select(c => new PostCommentProjection
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    ParentCommentId = c.ParentCommentId,
                    Content = c.Content,
                    ImageUrl = c.ImageUrl,
                    CreatedOn = c.CreatedOn,
                    UpdatedOn = c.UpdatedOn,
                    CreatedById = c.CreatedById,
                    UpdatedById = c.UpdatedById,
                    CreatorFirstName = c.CreatedBy != null ? c.CreatedBy.FirstName : null,
                    CreatorLastName = c.CreatedBy != null ? c.CreatedBy.LastName : null,
                    CreatorImageUrl = c.CreatedBy != null ? c.CreatedBy.ImageUrl : null
                })
                .ToList()
        });
    }

    /// <summary>
    /// Translates a <see cref="PostComment"/> query into a flat
    /// <see cref="PostCommentProjection"/> query (used by the comment endpoints).
    /// </summary>
    private static IQueryable<PostCommentProjection> ProjectComments(IQueryable<PostComment> comments)
    {
        return comments.Select(c => new PostCommentProjection
        {
            Id = c.Id,
            PostId = c.PostId,
            ParentCommentId = c.ParentCommentId,
            Content = c.Content,
            ImageUrl = c.ImageUrl,
            CreatedOn = c.CreatedOn,
            UpdatedOn = c.UpdatedOn,
            CreatedById = c.CreatedById,
            UpdatedById = c.UpdatedById,
            CreatorFirstName = c.CreatedBy != null ? c.CreatedBy.FirstName : null,
            CreatorLastName = c.CreatedBy != null ? c.CreatedBy.LastName : null,
            CreatorImageUrl = c.CreatedBy != null ? c.CreatedBy.ImageUrl : null
        });
    }

	private static PostResponse MapProjectionToResponse(PostProjection p, string baseUrl, string? currentUserId)
	{
		var repliesLookup = BuildRepliesLookup(p.Comments);
		var isCreatedByCurrentUser = !string.IsNullOrEmpty(currentUserId) && p.CreatedById == currentUserId;

		var rootComments = p.Comments
			.Where(c => c.ParentCommentId == null)
			.OrderBy(c => c.CreatedOn)
			.Select(c => MapCommentProjectionToResponse(c, repliesLookup, baseUrl, currentUserId: currentUserId))
			.ToList();

		return new PostResponse(
			p.Id,
			p.Title,
			p.Content,
			p.IsPublic,
			p.CourseId,
			p.SectionId,
			BuildImageUrl(p.ImageUrl, baseUrl),
			p.LikesCount,
			p.CommentsCount,
			p.CreatedOn,
			p.UpdatedOn,
			p.CreatedById,
			BuildFullName(p.CreatorFirstName, p.CreatorLastName),
			BuildImageUrl(p.CreatorImageUrl, baseUrl),
			isCreatedByCurrentUser,
			p.UpdatedById,
			p.IsLikedByCurrentUser,
			rootComments);
	}

	/// <summary>
	/// Iterative post-order traversal — assembles a comment + its full reply
	/// subtree without recursion and without re-sorting at each level.
	/// </summary>
	private static PostCommentResponse MapCommentProjectionToResponse(
		PostCommentProjection root,
		IReadOnlyDictionary<int, List<PostCommentProjection>> repliesLookup,
		string baseUrl,
		string? currentUserId)
	{
		var mapped = new Dictionary<int, PostCommentResponse>();
		var stack = new Stack<(PostCommentProjection node, bool processed)>();
		stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (node, processed) = stack.Pop();

			if (!processed)
			{
				stack.Push((node, true));
				if (repliesLookup.TryGetValue(node.Id, out var children))
				{
					for (var i = children.Count - 1; i >= 0; i--)
						stack.Push((children[i], false));
				}
			}
			else
			{
				List<PostCommentResponse> mappedChildren;
				if (repliesLookup.TryGetValue(node.Id, out var children))
				{
					mappedChildren = new List<PostCommentResponse>(children.Count);
					foreach (var c in children)
						mappedChildren.Add(mapped[c.Id]);
				}
				else
				{
					mappedChildren = new List<PostCommentResponse>(0);
				}
				var isCreatedByCurrentUser = !string.IsNullOrEmpty(currentUserId) && node.CreatedById == currentUserId;

				mapped[node.Id] = new PostCommentResponse(
					node.Id,
					node.PostId,
					node.ParentCommentId,
					node.Content,
					node.ImageUrl,
					node.CreatedOn,
					node.UpdatedOn,
					node.CreatedById,
					BuildFullName(node.CreatorFirstName, node.CreatorLastName),
					BuildImageUrl(node.CreatorImageUrl, baseUrl),
					isCreatedByCurrentUser,
					node.UpdatedById,
					mappedChildren);
			}
		}

        return mapped[root.Id];
    }

    private static Dictionary<int, List<PostCommentProjection>> BuildRepliesLookup(
        IEnumerable<PostCommentProjection> comments)
    {
        return comments
            .Where(c => c.ParentCommentId.HasValue)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(c => c.CreatedOn).ToList());
    }

    private static string BuildFullName(string? firstName, string? lastName)
    {
        var hasFirst = !string.IsNullOrWhiteSpace(firstName);
        var hasLast = !string.IsNullOrWhiteSpace(lastName);

        if (hasFirst && hasLast) return $"{firstName} {lastName}";
        if (hasFirst) return firstName!;
        if (hasLast) return lastName!;
        return string.Empty;
    }

    private static string? BuildImageUrl(string? relativePath, string baseUrl) =>
        string.IsNullOrEmpty(relativePath) ? null : $"{baseUrl}/{relativePath}";

    #endregion
}
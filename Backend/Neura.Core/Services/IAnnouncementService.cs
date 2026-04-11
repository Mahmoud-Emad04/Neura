using Neura.Core.Abstractions;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Contracts.Files;

namespace Neura.Core.Services;

public interface IAnnouncementService
{
    Task<Result<PaginatedList<PostResponse>>> GetAllPostsAsync(int pageNumber = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<Result<PostResponse>> GetPostByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<PostResponse>> CreatePostAsync(PostRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result> RemovePostAsync(int postId, string userId, CancellationToken cancellationToken = default);

    Task<Result<PostResponse>> UpdatePostAsync(int postId, PostUpdateRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result> UpdatePostImageAsync(int postId, UploadImageRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result<PostCommentResponse>> AddPostCommentAsync(int postId, PostCommentRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result> RemovePostCommentAsync(int commentId, string userId, CancellationToken cancellationToken = default);

    Task<Result<PostCommentResponse>> UpdatePostCommentAsync(int commentId, PostCommentUpdateRequest request,
        string userId, CancellationToken cancellationToken = default);

    Task<Result> UpdatePostCommentImageAsync(int commentId, UploadImageRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result> TogglePostLikeAsync(int postId, string userId, CancellationToken cancellationToken = default);
    Task<Result> TogglePostVisibilityAsync(int postId, string userId, CancellationToken cancellationToken = default);
}
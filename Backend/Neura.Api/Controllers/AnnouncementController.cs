using Neura.Core.Contracts.Announcement;
using Neura.Core.Contracts.Files;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[Route("api/announcements")]
[ApiController]
[Authorize]
public class AnnouncementController(IAnnouncementService announcementService, ILogger<AnnouncementController> logger)
    : ControllerBase
{
    private readonly IAnnouncementService _announcementService = announcementService;
    private readonly ILogger<AnnouncementController> _logger = logger;

    [HttpGet("posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPosts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _announcementService.GetAllPostsAsync(pageNumber, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("posts/{postId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostById([FromRoute] int postId, CancellationToken cancellationToken = default)
    {
        var result = await _announcementService.GetPostByIdAsync(postId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }


    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromForm] PostRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _announcementService.CreatePostAsync(request, userId, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPostById), new { postId = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("posts/{postId}")]
    public async Task<IActionResult> UpdatePost([FromRoute] int postId, [FromBody] PostUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _announcementService.UpdatePostAsync(postId, request, userId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> RemovePost([FromRoute] int postId, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _announcementService.RemovePostAsync(postId, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPut("posts/{postId}/visibility")]
    public async Task<IActionResult> TogglePostVisibility([FromRoute] int postId,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _announcementService.TogglePostVisibilityAsync(postId, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPut("posts/{postId}/image")]
    public async Task<IActionResult> UpdatePostImage([FromRoute] int postId, [FromForm] UploadImageRequest uploadImage,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _announcementService.UpdatePostImageAsync(postId, uploadImage, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPost("posts/{postId}/likes")]
    public async Task<IActionResult> TogglePostLike([FromRoute] int postId,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _announcementService.TogglePostLikeAsync(postId, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPost("posts/{postId}/comments")]
    public async Task<IActionResult> AddPostComment([FromRoute] int postId, [FromForm] PostCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _announcementService.AddPostCommentAsync(postId, request, userId, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPostById), new { postId }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("comments/{commentId}")]
    public async Task<IActionResult> UpdatePostComment([FromRoute] int commentId,
        [FromBody] PostCommentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _announcementService.UpdatePostCommentAsync(commentId, request, userId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("comments/{commentId}/image")]
    public async Task<IActionResult> UpdatePostCommentImage([FromRoute] int commentId,
        [FromForm] UploadImageRequest uploadImage, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result =
            await _announcementService.UpdatePostCommentImageAsync(commentId, uploadImage, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> RemovePostComment([FromRoute] int commentId,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _announcementService.RemovePostCommentAsync(commentId, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
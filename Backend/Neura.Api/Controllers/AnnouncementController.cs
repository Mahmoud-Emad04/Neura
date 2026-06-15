using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.Announcements.AddPostComment;
using Neura.Api.Features.Announcements.CreatePost;
using Neura.Api.Features.Announcements.GetAllPosts;
using Neura.Api.Features.Announcements.GetCurrentUserPosts;
using Neura.Api.Features.Announcements.GetPostById;
using Neura.Api.Features.Announcements.RemovePost;
using Neura.Api.Features.Announcements.RemovePostComment;
using Neura.Api.Features.Announcements.TogglePostLike;
using Neura.Api.Features.Announcements.TogglePostVisibility;
using Neura.Api.Features.Announcements.UpdatePost;
using Neura.Api.Features.Announcements.UpdatePostComment;
using Neura.Api.Features.Announcements.UpdatePostCommentImage;
using Neura.Api.Features.Announcements.UpdatePostImage;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Contracts.Files;

namespace Neura.Api.Controllers;

[Route("api/announcements")]
[ApiController]
[Authorize]
public class AnnouncementController(ISender sender) : ControllerBase
{
    [HttpGet("posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPosts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var query = new GetAllPostsQuery(pageNumber, pageSize, User.GetUserId() ?? null);
        var result = await sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("posts/my")]
    public async Task<IActionResult> GetCurrentUserPosts([FromQuery] bool? isPublic = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var query = new GetCurrentUserPostsQuery(User.GetUserId()!, isPublic, pageNumber, pageSize);
        var result = await sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("posts/{postId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostById([FromRoute] int postId, CancellationToken ct = default)
    {
        var query = new GetPostByIdQuery(postId, User.GetUserId());
        var result = await sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromForm] PostRequest request, CancellationToken ct = default)
    {
        var command = new CreatePostCommand(request, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPostById), new { postId = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("posts/{postId}")]
    public async Task<IActionResult> UpdatePost([FromRoute] int postId, [FromBody] PostUpdateRequest request, CancellationToken ct = default)
    {
        var command = new UpdatePostCommand(postId, request, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> RemovePost([FromRoute] int postId, CancellationToken ct = default)
    {
        var command = new RemovePostCommand(postId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPut("posts/{postId}/visibility")]
    public async Task<IActionResult> TogglePostVisibility([FromRoute] int postId, CancellationToken ct = default)
    {
        var command = new TogglePostVisibilityCommand(postId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPut("posts/{postId}/image")]
    public async Task<IActionResult> UpdatePostImage([FromRoute] int postId, [FromForm] UploadImageRequest uploadImage, CancellationToken ct = default)
    {
        var command = new UpdatePostImageCommand(postId, uploadImage, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPost("posts/{postId}/likes")]
    public async Task<IActionResult> TogglePostLike([FromRoute] int postId, CancellationToken ct = default)
    {
        var command = new TogglePostLikeCommand(postId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPost("posts/{postId}/comments")]
    public async Task<IActionResult> AddPostComment([FromRoute] int postId, [FromForm] PostCommentRequest request, CancellationToken ct = default)
    {
        var command = new AddPostCommentCommand(postId, request, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPostById), new { postId }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("comments/{commentId}")]
    public async Task<IActionResult> UpdatePostComment([FromRoute] int commentId, [FromBody] PostCommentUpdateRequest request, CancellationToken ct = default)
    {
        var command = new UpdatePostCommentCommand(commentId, request, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut("comments/{commentId}/image")]
    public async Task<IActionResult> UpdatePostCommentImage([FromRoute] int commentId, [FromForm] UploadImageRequest uploadImage, CancellationToken ct = default)
    {
        var command = new UpdatePostCommentImageCommand(commentId, uploadImage, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> RemovePostComment([FromRoute] int commentId, CancellationToken ct = default)
    {
        var command = new RemovePostCommentCommand(commentId, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }
}
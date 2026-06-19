using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.Lessons.AskChatbot;
using Neura.Api.Features.Lessons.CreateLessonMetadata;
using Neura.Api.Features.Lessons.GetChatHistory;
using Neura.Api.Features.Lessons.DeleteLesson;
using Neura.Api.Features.Lessons.GetArticleContent;
using Neura.Api.Features.Lessons.GetSectionLessons;
using Neura.Api.Features.Lessons.MarkCompleted;
using Neura.Api.Features.Lessons.UpdateArticleContent;
using Neura.Api.Features.Lessons.UpdateLesson;
using Neura.Api.Features.Lessons.UpdateLessonPosition;
using Neura.Api.Features.Lessons.UpdateLessonPrivacy;
using Neura.Api.Features.Lessons.Video.FinalizeVideoUpload;
using Neura.Api.Features.Lessons.Video.GetSignedVideoUpload;
using Neura.Api.Features.Lessons.Video.GetVideoLink;
using Neura.Core.Authorization.Attributes;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Enums;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LessonsController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     PAGE 1: Initialize the lesson shell with basic metadata.
    /// </summary>
    [HttpPost("{sectionId}/init")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HasSectionPermission(CoursePermission.EditContent)]
    public async Task<IActionResult> Initialize(
        [FromRoute] int sectionId,
        [FromBody] CreateLessonRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new CreateLessonMetadataCommand(sectionId, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(new { LessonId = result.Value })
            : result.ToProblem();
    }

    /// <summary>
    ///     Updates the position of a lesson within its section.
    ///     Route: PUT /api/lessons/{id}/position
    /// </summary>
    [HttpPut("{id}/position")]
    [HasLessonPermission(CoursePermission.EditContent)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePosition(
        [FromRoute] int id,
        [FromBody] UpdateLessonPositionRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new UpdateLessonPositionCommand(id, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Updates the privacy status of a lesson.
    ///     Route: PUT /api/lessons/{id}/privacy
    /// </summary>
    [HttpPut("{id}/privacy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HasLessonPermission(CoursePermission.EditContent)]
    public async Task<IActionResult> UpdatePrivacy(
        [FromRoute] int id,
        [FromBody] UpdateLessonPrivacyRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new UpdateLessonPrivacyCommand(id, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Updates lesson information (title, description, etc).
    ///     Route: PUT /api/lessons/{id}
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HasLessonPermission(CoursePermission.EditContent)]
    public async Task<IActionResult> UpdateLesson(
        [FromRoute] int id,
        [FromBody] UpdateLessonRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new UpdateLessonCommand(id, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Gets all lessons in a section with position information.
    ///     Route: GET /api/lessons/section/{sectionId}
    /// </summary>
    [HttpGet("section/{sectionId}")]
    [ProducesResponseType(typeof(List<LessonWithPositionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSectionLessons(
        [FromRoute] int sectionId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetSectionLessonsQuery(sectionId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Updates the HTML content for an Article-type lesson.
    /// </summary>
    [HttpPut("{id}/article")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HasLessonPermission(CoursePermission.EditContent)]
    public async Task<IActionResult> UpdateArticle(
        [FromRoute] int id,
        [FromBody] UpdateArticleRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new UpdateArticleContentCommand(id, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Retrieves the HTML content of an Article-type lesson.
    /// </summary>
    [HttpGet("{id}/article")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticle(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetArticleContentQuery(id, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Retrieves the video link URL for a lesson with access control validation.
    ///     Route: GET /api/lessons/{id}/video/link
    /// </summary>
    /// <param name="id">The lesson ID to get video link for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Video URL, duration, and privacy status if user is authorized.</returns>
    [HttpGet("{id}/video/link")]
    [ProducesResponseType(typeof(VideoLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetVideoLink(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetVideoLinkQuery(id, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Generates signed upload credentials for secure direct Cloudinary video upload.
    ///     Route: POST /api/lessons/{id}/video/signed-upload
    /// </summary>
    /// <param name="id">The lesson ID to upload video for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Signed upload credentials for client to use with Cloudinary.</returns>
    [HttpPost("{id}/video/signed-upload")]
    [ProducesResponseType(typeof(SignedVideoUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
    [HasLessonPermission(CoursePermission.EditContent)]
    public async Task<IActionResult> GetSignedVideoUploadCredentials(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new GetSignedVideoUploadCommand(id, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Finalizes video upload by linking Cloudinary video to lesson.
    ///     Route: POST /api/lessons/{id}/video/finalize
    /// </summary>
    /// <param name="id">The lesson ID to link video to.</param>
    /// <param name="request">Finalization details (public ID, URL, duration).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Confirmation with linked video details.</returns>
    [HttpPost("{id}/video/finalize")]
    [ProducesResponseType(typeof(FinalizeVideoUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
    [HasLessonPermission(CoursePermission.EditContent)]
    public async Task<IActionResult> FinalizeVideoUpload(
        [FromRoute] int id,
        [FromBody] FinalizeVideoUploadRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new FinalizeVideoUploadCommand(id, request, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Deletes lesson.
    ///     Route: DELETE /api/lessons/{id}
    /// </summary>
    /// <param name="id">The lesson ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>NoContent on success.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
    [HasLessonPermission(CoursePermission.EditContent)]
    public async Task<IActionResult> DeleteVideo(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new DeleteLessonCommand(id, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    [HttpPost("~/api/LessonProgres/lessons/{lessonId:int}/complete")]
    [ProducesResponseType(typeof(LessonCompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkCompleted(
        [FromRoute] int lessonId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var command = new MarkLessonCompletedCommand(lessonId, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpPost("{lessonId}/chat")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AskChatbot(
        [FromRoute] int lessonId,
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var isAssistant = User.IsInRole("Instructor") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        var userRole = isAssistant ? "assistant" : "user";
        
        var command = new AskChatbotCommand(lessonId, request.Question, userId, userRole);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }

    [HttpGet("{lessonId}/chat")]
    [ProducesResponseType(typeof(IEnumerable<ChatHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChatHistory(
        [FromRoute] int lessonId,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        var query = new GetChatHistoryQuery(lessonId, userId);
        var result = await sender.Send(query, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}
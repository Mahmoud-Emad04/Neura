using Neura.Api.Extensions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LessonsController(
    ILessonService lessonService,
    ICloudinaryService cloudinaryService) : ControllerBase
{
    private readonly ILessonService _lessonService = lessonService;
    private readonly ICloudinaryService _cloudinaryService = cloudinaryService;

    /// <summary>
    ///     PAGE 1: Initialize the lesson shell with basic metadata.
    /// </summary>
    [HttpPost("init")]
    [Authorize]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Initialize([FromBody] CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lessonService.CreateLessonMetadataAsync(request, cancellationToken);

        return result.IsSuccess
            ? Ok(new { LessonId = result.Value })
            : result.ToProblem();
    }

    /// <summary>
    ///     PAGE 2: Upload the video and provide final details to publish the lesson.
    /// </summary>
    /// <remarks>
    ///     Uses 'FromForm' to handle the multi-part request (File + JSON fields).
    /// </remarks>
    [HttpPut("{id}/complete")]
    [Authorize]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_073_741_824)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        int id,
        [FromForm] CompleteLessonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lessonService.CompleteLessonAsync(id, request, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Gets the Cloudinary video URL for a lesson with appropriate access control.
    ///     For private videos, returns a signed URL valid for 1 hour.
    /// </summary>
    [HttpGet("{id}/video")]
    [Authorize]
    [ProducesResponseType(typeof(CloudinaryVideoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVideoUrl(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _lessonService.GetCloudinaryVideoAsync(id, userId, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Streams video directly from Cloudinary through the backend.
    ///     This endpoint validates token expiration, access control, and prevents downloads.
    /// </summary>
    [HttpGet("{id}/stream")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> StreamVideo(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        // Get the video URL and validate access (this does the heavy lifting of security checks)
        var result = await _lessonService.GetCloudinaryVideoAsync(id, userId, ct);

        if (result.IsFailure)
            return result.ToProblem();

        var videoUrl = result.Value.Url;

        try
        {
            // Security: Set headers to prevent caching and downloading
            Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Content-Disposition", "inline");     // Force inline display, not download
            Response.Headers.Append("X-Content-Type-Options", "nosniff"); // Security header

            // Set CSP to restrict where this video can be embedded (optional but recommended)
            // Response.Headers.Append("Content-Security-Policy", "default-src 'self'; media-src 'self' blob:;");

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };

            // Add a small trick to prevent simple downloaders by requiring specific headers
            // (Cloudinary doesn't care, but we can log/filter later if needed)

            var response = await httpClient.GetAsync(videoUrl, HttpCompletionOption.ResponseHeadersRead, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return Unauthorized(new { message = "Video access token expired. Please request a new one." });

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, new { message = "Failed to stream video from Cloudinary" });

            var stream = await response.Content.ReadAsStreamAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "video/mp4";

            // Required for progressive streaming (seeking in video)
            Response.Headers.Append("Accept-Ranges", "bytes");

            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout, new { message = "Video streaming request timed out or was cancelled by the client." });
        }
        catch (Exception ex)
        {
            // Log exception here if you have a logger
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error streaming video." });
        }
    }

    /// <summary>
    ///     Updates the position of a lesson within its section.
    ///     Route: PUT /api/lessons/{id}/position
    /// </summary>
    [HttpPut("{id}/position")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePosition(int id, [FromBody] UpdateLessonPositionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _lessonService.UpdateLessonPositionAsync(id, request.NewPosition, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Updates the privacy status of a lesson.
    ///     Route: PUT /api/lessons/{id}/privacy
    /// </summary>
    [HttpPut("{id}/privacy")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrivacy(int id, [FromBody] UpdateLessonPrivacyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _lessonService.UpdateLessonPrivacyAsync(id, request, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Updates lesson information (title, description, etc).
    ///     Route: PUT /api/lessons/{id}
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLesson(int id, [FromBody] UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _lessonService.UpdateLessonAsync(id, request, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Deletes a lesson from a section.
    ///     Route: DELETE /api/lessons/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLesson(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _lessonService.DeleteLessonAsync(id, userId, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblem();
    }

    /// <summary>
    ///     Gets all lessons in a section with position information.
    ///     Route: GET /api/lessons/section/{sectionId}
    /// </summary>
    [HttpGet("section/{sectionId}")]
    [ProducesResponseType(typeof(List<LessonWithPositionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSectionLessons(int sectionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _lessonService.GetSectionLessonsAsync(sectionId, userId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }
}

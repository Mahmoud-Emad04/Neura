using Neura.Api.Extensions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LessonsController(
    ILessonService lessonService,
    IFileService fileService,
    ICloudinaryService cloudinaryService) : ControllerBase
{
    private readonly IFileService _fileService = fileService;
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

    [HttpGet("{id}/stream")]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> StreamVideo(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var result = await _lessonService.GetLessonVideoPathAsync(id, userId, ct);

        if (result.IsFailure) return result.ToProblem();

        var (physicalPath, contentType) = result.Value;

        return PhysicalFile(physicalPath, contentType, true);
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

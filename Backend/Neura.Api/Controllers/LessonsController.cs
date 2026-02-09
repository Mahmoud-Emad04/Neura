using Neura.Api.Extensions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LessonsController(ILessonService lessonService, IFileService fileService) : ControllerBase
{
    private readonly ILessonService _lessonService = lessonService;
    private readonly IFileService _fileService = fileService;

    /// <summary>
    /// PAGE 1: Initialize the lesson shell with basic metadata.
    /// </summary>
    [HttpPost("init")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Initialize([FromBody] CreateLessonRequest request, CancellationToken cancellationToken)
    {
        var result = await _lessonService.CreateLessonMetadataAsync(request, cancellationToken);

        return result.IsSuccess
            ? Ok(new { LessonId = result.Value })
            : result.ToProblem();
    }

    /// <summary>
    /// PAGE 2: Upload the video and provide final details to publish the lesson.
    /// </summary>
    /// <remarks>
    /// Uses 'FromForm' to handle the multi-part request (File + JSON fields).
    /// </remarks>
    [HttpPut("{id}/complete")]
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

        return PhysicalFile(physicalPath, contentType, enableRangeProcessing: true);
    }
}

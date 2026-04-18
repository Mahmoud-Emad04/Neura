using CloudinaryDotNet;
using Neura.Api.Extensions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LessonsController(
    ILessonService lessonService) : ControllerBase
{
    private readonly ILessonService _lessonService = lessonService;
	private readonly Cloudinary _cloudinary;

	/// <summary>
	///     PAGE 1: Initialize the lesson shell with basic metadata.
	/// </summary>
	[HttpPost("init")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Initialize([FromBody] CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lessonService.CreateLessonMetadataAsync(request, User.GetUserId()!, cancellationToken);

        return result.IsSuccess
            ? Ok(new { LessonId = result.Value })
            : result.ToProblem();
    }

    /// <summary>
    ///     Updates the position of a lesson within its section.
    ///     Route: PUT /api/lessons/{id}/position
    /// </summary>
    [HttpPut("{id}/position")]
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
    ///     Gets all lessons in a section with position information.
    ///     Route: GET /api/lessons/section/{sectionId}
    /// </summary>
    [HttpGet("section/{sectionId}")]
    [ProducesResponseType(typeof(List<LessonWithPositionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSectionLessons(int sectionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId()!;
        var result = await _lessonService.GetSectionLessonsAsync(sectionId, userId, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Updates the HTML content for an Article-type lesson.
    /// </summary>
    [HttpPut("{id}/article")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateArticle(int id, [FromBody] UpdateArticleRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId()!;

        var result = await _lessonService.UpdateArticleContentAsync(id, request, userId, ct);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }

    /// <summary>
    ///     Retrieves the HTML content of an Article-type lesson.
    /// </summary>
    [HttpGet("{id}/article")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticle(int id, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var result = await _lessonService.GetArticleContentAsync(id, userId!, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblem();
    }
}
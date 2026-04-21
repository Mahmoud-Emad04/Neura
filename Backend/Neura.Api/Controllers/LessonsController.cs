using Neura.Api.Extensions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LessonsController(
	ILessonService lessonService,
	IVideoService videoService) : ControllerBase
{
	private readonly ILessonService _lessonService = lessonService;
	private readonly IVideoService _videoService = videoService;

	/// <summary>
	///     PAGE 1: Initialize the lesson shell with basic metadata.
	/// </summary>
	[HttpPost("{sectionId}/init")]
	[ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Initialize([FromRoute] int sectionId, [FromBody] CreateLessonRequest request,
		CancellationToken cancellationToken)
	{
		var result = await _lessonService.CreateLessonMetadataAsync(sectionId,request, User.GetUserId()!, cancellationToken);

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
		var result = await _lessonService.UpdateLessonPrivacyAsync(id, request, userId!, cancellationToken);

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

	/// <summary>
	///     Retrieves the video link URL for a lesson with access control validation.
	///     Route: GET /api/lessons/{id}/video/link
	/// </summary>
	/// <param name="id">The lesson ID to get video link for.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Video URL, duration, and privacy status if user is authorized.</returns>
	[HttpGet("{id}/video/link")]
	[ProducesResponseType(typeof(VideoLinkResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> GetVideoLink(int id, CancellationToken cancellationToken)
	{
		var userId = User.GetUserId();
		var result = await _videoService.GetVideoLinkAsync(id, userId!, cancellationToken);

		return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
	}

	/// <summary>
	///     Generates signed upload credentials for secure direct Cloudinary video upload.
	///     Route: POST /api/lessons/{id}/video/signed-upload
	/// </summary>
	/// <param name="id">The lesson ID to upload video for.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Signed upload credentials for client to use with Cloudinary.</returns>
	[HttpPost("{id}/video/signed-upload")]
	[ProducesResponseType(typeof(SignedVideoUploadResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> GetSignedVideoUploadCredentials(int id, CancellationToken cancellationToken)
	{
		var userId = User.GetUserId();
		var result = await _videoService.GetSignedUploadCredentialsAsync(id, userId!, cancellationToken);

		return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
	}

	/// <summary>
	///     Finalizes video upload by linking Cloudinary video to lesson.
	///     Route: POST /api/lessons/{id}/video/finalize
	/// </summary>
	/// <param name="id">The lesson ID to link video to.</param>
	/// <param name="request">Finalization details (public ID, URL, duration).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Confirmation with linked video details.</returns>
	[HttpPost("{id}/video/finalize")]
	[ProducesResponseType(typeof(FinalizeVideoUploadResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> FinalizeVideoUpload(
		int id,
		[FromBody] FinalizeVideoUploadRequest request,
		CancellationToken cancellationToken)
	{
		var userId = User.GetUserId();
		var result = await _videoService.FinalizeUploadAsync(id, request, userId!, cancellationToken);

		return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
	}

	/// <summary>
	///     Deletes a video from a lesson.
	///     Route: DELETE /api/lessons/{id}/video
	/// </summary>
	/// <param name="id">The lesson ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>NoContent on success.</returns>
	[HttpDelete("{id}/video")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> DeleteVideo(int id, CancellationToken cancellationToken)
	{
		var userId = User.GetUserId();
		var result = await _videoService.DeleteVideoAsync(id, userId!, cancellationToken);

		return result.IsSuccess ? NoContent() : result.ToProblem();
	}

}
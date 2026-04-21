using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Core.Services;

/// <summary>
/// Service for managing video uploads and Cloudinary operations for lessons.
/// </summary>
public interface IVideoService
{
	/// <summary>
	/// Generates signed upload credentials for secure direct Cloudinary upload.
	/// Returns signature, timestamp, and other parameters needed by client.
	/// </summary>
	/// <param name="lessonId">The lesson to upload video for.</param>
	/// <param name="userId">The user requesting upload (must be lesson author).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Signed upload credentials for client to use with Cloudinary.</returns>
	Task<Result<SignedVideoUploadResponse>> GetSignedUploadCredentialsAsync(
		int lessonId,
		string userId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Finalizes video upload by linking Cloudinary upload to lesson.
	/// Validates upload success, optionally deletes old video, and updates lesson record.
	/// </summary>
	/// <param name="lessonId">The lesson to link video to.</param>
	/// <param name="request">Upload finalization details (public ID, URL, duration).</param>
	/// <param name="userId">The user finalizing upload (must be lesson author).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Confirmation with linked video details.</returns>
	Task<Result<FinalizeVideoUploadResponse>> FinalizeUploadAsync(
		int lessonId,
		FinalizeVideoUploadRequest request,
		string userId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes a video from a lesson and from Cloudinary.
	/// </summary>
	/// <param name="lessonId">The lesson to delete video from.</param>
	/// <param name="userId">The user requesting deletion (must be lesson author).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Success or failure result.</returns>
	Task<Result> DeleteVideoAsync(
		int lessonId,
		string userId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves the video link URL for a lesson with access control.
	/// Validates that the lesson is published, has a video, and user has access.
	/// </summary>
	/// <param name="lessonId">The lesson to get video link for.</param>
	/// <param name="userId">The user requesting the video link.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Video URL, duration, and privacy status if authorized.</returns>
	Task<Result<VideoLinkResponse>> GetVideoLinkAsync(
		int lessonId,
		string userId,
		CancellationToken cancellationToken = default);
}

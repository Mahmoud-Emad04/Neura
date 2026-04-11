using Neura.Core.Abstractions;

namespace Neura.Core.Services;

/// <summary>
///     Interface for Cloudinary video upload and management service.
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    ///     Uploads a video file to Cloudinary.
    /// </summary>
    /// <param name="videoFile">The video file to upload</param>
    /// <param name="lessonId">The lesson ID for organizing uploads</param>
    /// <param name="isPrivate">If true, video is accessible only to enrolled students. If false, preview mode (public)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cloudinary video URL</returns>
    Task<Result<string>> UploadVideoAsync(IFormFile videoFile, int lessonId, bool isPrivate,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates a signed URL for private videos (enrolled students only).
    /// </summary>
    /// <param name="videoUrl">The original Cloudinary video URL</param>
    /// <param name="expirationSeconds">Token expiration time in seconds (default: 3600 = 1 hour)</param>
    /// <returns>Signed URL for temporary access</returns>
    string GenerateSignedUrl(string videoUrl, int expirationSeconds = 3600);

    /// <summary>
    ///     Deletes a video from Cloudinary.
    /// </summary>
    /// <param name="videoPublicId">The Cloudinary public ID of the video</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result> DeleteVideoAsync(string videoPublicId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets video metadata from Cloudinary.
    /// </summary>
    /// <param name="videoPublicId">The Cloudinary public ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result<CloudinaryVideoMetadata>> GetVideoMetadataAsync(string videoPublicId,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Represents metadata for a Cloudinary video.
/// </summary>
public record CloudinaryVideoMetadata(
    string PublicId,
    string Url,
    long? Duration,
    int Width,
    int Height,
    string Format);
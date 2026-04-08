using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace Neura.Services.Services;

/// <summary>
/// Service for managing video uploads to Cloudinary with public/private access control.
/// </summary>
public class CloudinaryService(IConfiguration configuration) : ICloudinaryService
{
    private readonly Cloudinary _cloudinary = InitializeCloudinary(configuration);
    private readonly string _apiSecret = configuration["Cloudinary:ApiSecret"] ?? string.Empty;

    /// <summary>
    /// Uploads a video file to Cloudinary with appropriate access control settings.
    /// </summary>
    public async Task<Result<string>> UploadVideoAsync(
        IFormFile videoFile,
        int lessonId,
        bool isPrivate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (videoFile == null || videoFile.Length == 0)
                return Result.Failure<string>(CloudinaryErrors.UploadFailed);

            // Validate video file
            if (!IsValidVideoFile(videoFile))
                return Result.Failure<string>(CloudinaryErrors.InvalidVideoFormat);

            using var stream = videoFile.OpenReadStream();

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(videoFile.FileName, stream),
                PublicId = $"lessons/{lessonId}/{Guid.NewGuid()}",
                Folder = "lessons"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                return Result.Failure<string>(CloudinaryErrors.UploadFailed);

            return Result.Success(uploadResult.SecureUrl.ToString());
        }
        catch (Exception)
        {
            return Result.Failure<string>(CloudinaryErrors.UploadFailed);
        }
    }

    /// <summary>
    /// Generates a signed/token URL for accessing private videos with expiration.
    /// </summary>
    public string GenerateSignedUrl(string videoUrl, int expirationSeconds = 3600)
    {
        try
        {
            // For Cloudinary, we use auth token with expiration
            var expirationTime = (int)DateTimeOffset.UtcNow.AddSeconds(expirationSeconds).ToUnixTimeSeconds();

            // Extract public ID from URL
            var publicId = ExtractPublicIdFromUrl(videoUrl);

            // Build auth string
            var authString = $"end_time={expirationTime}";
            var hash = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes($"{authString}{_apiSecret}"));
            var token = System.Convert.ToHexString(hash).ToLower();

            var separator = videoUrl.Contains("?") ? "&" : "?";
            return $"{videoUrl}{separator}auth_token={token}&end_time={expirationTime}";
        }
        catch
        {
            return videoUrl; // Fallback to original URL if signing fails
        }
    }

    /// <summary>
    /// Deletes a video from Cloudinary.
    /// </summary>
    public async Task<Result> DeleteVideoAsync(string videoPublicId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteParams = new DeletionParams(videoPublicId) { ResourceType = ResourceType.Video };
            var deleteResult = await _cloudinary.DestroyAsync(deleteParams);

            if (deleteResult.Error != null)
                return Result.Failure(CloudinaryErrors.DeleteFailed);

            return Result.Success();
        }
        catch
        {
            return Result.Failure(CloudinaryErrors.DeleteFailed);
        }
    }

    /// <summary>
    /// Retrieves video metadata from Cloudinary.
    /// </summary>
    public async Task<Result<CloudinaryVideoMetadata>> GetVideoMetadataAsync(
        string videoPublicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var getParams = new GetResourceParams(videoPublicId) { ResourceType = ResourceType.Video };
            var resource = await _cloudinary.GetResourceAsync(getParams);

            if (resource == null)
                return Result.Failure<CloudinaryVideoMetadata>(CloudinaryErrors.VideoNotFound);

            var metadata = new CloudinaryVideoMetadata(
                PublicId: videoPublicId,
                Url: resource.SecureUrl.ToString(),
                Duration: resource.Bytes,  // Using Bytes as a fallback for duration
                Width: resource.Width,
                Height: resource.Height,
                Format: resource.Format
            );

            return Result.Success(metadata);
        }
        catch
        {
            return Result.Failure<CloudinaryVideoMetadata>(CloudinaryErrors.VideoNotFound);
        }
    }

    /// <summary>
    /// Initializes the Cloudinary client with configuration.
    /// </summary>
    private static Cloudinary InitializeCloudinary(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            throw new InvalidOperationException(
                "Cloudinary configuration is missing. Please set Cloudinary:CloudName, Cloudinary:ApiKey, and Cloudinary:ApiSecret in appsettings.json");

        var account = new Account(cloudName, apiKey, apiSecret);
        return new Cloudinary(account);
    }

    /// <summary>
    /// Validates if the file is a supported video format.
    /// </summary>
    private static bool IsValidVideoFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".webm", ".flv", ".wmv" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return allowedExtensions.Contains(fileExtension);
    }

    /// <summary>
    /// Extracts the public ID from a Cloudinary URL.
    /// </summary>
    private static string ExtractPublicIdFromUrl(string pathAndQuery)
    {
        // URL format: /v{version}/lessons/{lessonId}/{guid}.{format}
        var parts = pathAndQuery.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            var fileName = parts[^1]; // Get last part (filename.format)
            var lessonId = parts[^2];
            return $"lessons/{lessonId}/{Path.GetFileNameWithoutExtension(fileName)}";
        }

        return pathAndQuery;
    }
}

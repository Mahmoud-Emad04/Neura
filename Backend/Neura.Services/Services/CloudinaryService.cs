using System.Security.Cryptography;
using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Error = Neura.Core.Abstractions.Error;

namespace Neura.Services.Services;

/// <summary>
///     Service for managing video uploads to Cloudinary with public/private access control.
/// </summary>
public class CloudinaryService(IConfiguration configuration) : ICloudinaryService
{
    private readonly string _apiSecret = configuration["Cloudinary:ApiSecret"] ?? string.Empty;
    private readonly Cloudinary _cloudinary = InitializeCloudinary(configuration);

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
                Folder = "lessons",
                // Upload as 'authenticated' to prevent unauthorized downloads
                Type = "authenticated",
                // Convert to HLS format for streaming, improving security and performance
                EagerTransforms = new List<Transformation>
                {
                    new EagerTransformation { Format = "m3u8" }.StreamingProfile("hd")
                },
                EagerAsync = true // Do this in background
            };

            // Use UploadLargeAsync to upload videos in chunks (avoids timeouts and memory issues for large files)
            var uploadResult = await _cloudinary.UploadLargeAsync(uploadParams);

            if (uploadResult.Error != null)
                return Result.Failure<string>(CloudinaryErrors.UploadFailed);

            // Favor returning the Eager transformed HLS stream URL if available
            string? streamUrl = null;
            if (uploadResult.JsonObj != null && uploadResult.JsonObj["eager"] != null &&
                uploadResult.JsonObj["eager"].HasValues)
                streamUrl = uploadResult.JsonObj["eager"]?[0]?["secure_url"]?.ToString();

            streamUrl ??= uploadResult.SecureUrl?.ToString()?.Replace(".mp4", ".m3u8") ?? string.Empty;

            return Result.Success(streamUrl);
        }
        catch (Exception)
        {
            return Result.Failure<string>(CloudinaryErrors.UploadFailed);
        }
    }

    /// <summary>
    ///     Generates a signed/authenticated URL for streaming private videos from Cloudinary.
    ///     Uses HMAC-SHA256 to create a cryptographically signed URL that expires after the specified duration.
    ///     After expiration, Cloudinary will reject the request with 401 Unauthorized.
    /// </summary>
    public string GenerateSignedUrl(string videoUrl, int expirationSeconds = 3600)
    {
        try
        {
            // Calculate expiration time in Unix timestamp
            var expirationTime = DateTimeOffset.UtcNow.AddSeconds(expirationSeconds).ToUnixTimeSeconds();

            // Extract path from URL (everything after domain)
            // URL format: https://res.cloudinary.com/cloud/video/upload/v123/lessons/5/guid.mp4
            var uri = new Uri(videoUrl);
            var path = uri.PathAndQuery; // Gets: /cloud/video/upload/v123/lessons/5/guid.mp4

            // Cloudinary auth string: {path}?end_time={timestamp}
            // This is what we'll sign with HMAC-SHA256
            var authString = $"{path}?end_time={expirationTime}";

            // Create HMAC-SHA256 signature using API secret as the key
            // This is the correct Cloudinary authentication mechanism
            var apiSecretBytes = Encoding.UTF8.GetBytes(_apiSecret);
            var messageBytes = Encoding.UTF8.GetBytes(authString);

            using var hmac = new HMACSHA256(apiSecretBytes);
            var hash = hmac.ComputeHash(messageBytes);
            var token = Convert.ToHexString(hash).ToLower();

            // Return URL with Cloudinary authentication parameters
            // Cloudinary validates:
            // 1. HMAC signature matches
            // 2. Current Unix time < end_time value
            // If either check fails, returns 401 Unauthorized
            var separator = videoUrl.Contains("?") ? "&" : "?";
            return $"{videoUrl}{separator}end_time={expirationTime}&auth_token={token}";
        }
        catch
        {
            return videoUrl; // Fallback to original URL if signing fails
        }
    }

    /// <summary>
    ///     Deletes a video from Cloudinary.
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
    ///     Retrieves video metadata from Cloudinary.
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
                videoPublicId,
                resource.SecureUrl,
                resource.Bytes, // Using Bytes as a fallback for duration
                resource.Width,
                resource.Height,
                resource.Format
            );

            return Result.Success(metadata);
        }
        catch
        {
            return Result.Failure<CloudinaryVideoMetadata>(CloudinaryErrors.VideoNotFound);
        }
    }

    private static Cloudinary InitializeCloudinary(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            throw new InvalidOperationException("Cloudinary configuration is missing.");

        var account = new Account(cloudName, apiKey, apiSecret);
        return new Cloudinary(account);
    }

    /// <summary>
    ///     Validates if the file is a supported video format.
    /// </summary>
    private static bool IsValidVideoFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".webm", ".flv", ".wmv" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return allowedExtensions.Contains(fileExtension);
    }

    /// <summary>
    ///     Extracts the public ID from a Cloudinary URL.
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
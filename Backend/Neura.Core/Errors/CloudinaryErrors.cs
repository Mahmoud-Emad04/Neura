using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class CloudinaryErrors
{
    public static readonly Error UploadFailed = new(
        "Cloudinary.UploadFailed",
        "Failed to upload video to Cloudinary",
        StatusCodes.Status500InternalServerError);

    public static readonly Error InvalidVideoFormat = new(
        "Cloudinary.InvalidVideoFormat",
        "Invalid video format. Allowed formats: mp4, avi, mov, mkv, webm",
        StatusCodes.Status400BadRequest);

    public static readonly Error FileTooLarge = new(
        "Cloudinary.FileTooLarge",
        "Video file exceeds maximum allowed size",
        StatusCodes.Status413PayloadTooLarge);

    public static readonly Error DeleteFailed = new(
        "Cloudinary.DeleteFailed",
        "Failed to delete video from Cloudinary",
        StatusCodes.Status500InternalServerError);

    public static readonly Error VideoNotFound = new(
        "Cloudinary.VideoNotFound",
        "Video not found in Cloudinary",
        StatusCodes.Status404NotFound);

    public static readonly Error SigningFailed = new(
        "Cloudinary.SigningFailed",
        "Failed to generate signed URL for private video",
        StatusCodes.Status500InternalServerError);
}

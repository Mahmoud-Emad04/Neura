namespace Neura.Core.Contracts.Lessons;

/// <summary>
/// Response containing Cloudinary video information with access control details.
/// </summary>
public record CloudinaryVideoResponse(
    string Url,
    string? SignedUrl,
    bool IsPrivate,
    bool IsPreview,
    int Duration,
    DateTime? ExpiresAt
);

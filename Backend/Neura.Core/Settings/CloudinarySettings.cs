namespace Neura.Core.Settings;

/// <summary>
/// Configuration settings for Cloudinary service.
/// </summary>
public class CloudinarySettings
{
	public const string SectionName = "Cloudinary";

	/// <summary>
	/// Cloudinary cloud name (account identifier).
	/// </summary>
	public string CloudName { get; set; } = string.Empty;

	/// <summary>
	/// Cloudinary API key (public).
	/// </summary>
	public string ApiKey { get; set; } = string.Empty;

	/// <summary>
	/// Cloudinary API secret (private - keep secure).
	/// </summary>
	public string ApiSecret { get; set; } = string.Empty;

	/// <summary>
	/// Folder name for storing videos in Cloudinary.
	/// </summary>
	public string FolderName { get; set; } = "Neura/Lessons";

	/// <summary>
	/// Video upload size limit in MB (default: 500MB).
	/// </summary>
	public int MaxVideoSizeMB { get; set; } = 500;

	/// <summary>
	/// Video upload URL expiration in minutes (default: 60 minutes).
	/// </summary>
	public int SignatureExpirationMinutes { get; set; } = 60;

	/// <summary>
	/// Allowed video formats (comma-separated).
	/// </summary>
	public string AllowedFormats { get; set; } = "mp4,webm,mov,avi";

	/// <summary>
	/// Validate settings have required values.
	/// </summary>
	public bool IsValid() =>
		!string.IsNullOrWhiteSpace(CloudName) &&
		!string.IsNullOrWhiteSpace(ApiKey) &&
		!string.IsNullOrWhiteSpace(ApiSecret);
}

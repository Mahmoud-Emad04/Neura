using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Settings;
using Error = Neura.Core.Abstractions.Error;

namespace Neura.Services.Services;

public class VideoService(
    Cloudinary cloudinary,
    CloudinarySettings cloudinarySettings,
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<VideoService> logger) : IVideoService
{
    private readonly Cloudinary _cloudinary = cloudinary;
    private readonly CloudinarySettings _settings = cloudinarySettings;
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<VideoService> _logger = logger;


    public async Task<Result<SignedVideoUploadResponse>> GetSignedUploadCredentialsAsync(
    int lessonId,
    string userId,
    CancellationToken cancellationToken = default)
    {
        // Verify lesson exists and user has permission to upload
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section).ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure<SignedVideoUploadResponse>(LessonErrors.NotFound);

        // Check authorization: user must be course owner or superadmin
        var isAuthorized = await IsUserAuthorizedAsync(lessonId, userId, cancellationToken);
        if (!isAuthorized)
            return Result.Failure<SignedVideoUploadResponse>(LessonErrors.UnauthorizedModification);

        // Generate unique ID for Neura's naming convention and a clean Unix timestamp
        var publicId = $"lesson_{lessonId}_{Guid.NewGuid():N}";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(_settings.SignatureExpirationMinutes).ToUnixTimeSeconds();

        // 1. ONLY include parameters the frontend will actually send in the form-data 
        // (Excluding 'file' and 'api_key' which are never signed)
        var uploadParams = new Dictionary<string, object>
    {
        { "folder", _settings.FolderName },
        { "public_id", publicId },
        { "timestamp", timestamp }
    };

        // 2. Use the official SDK to generate the signature automatically
        // It automatically uses the ApiSecret configured when you registered the Cloudinary instance
        var signature = _cloudinary.Api.SignParameters(uploadParams);

        try
        {
            var response = new SignedVideoUploadResponse(
                CloudName: _settings.CloudName,
                UploadUrl: $"https://api.cloudinary.com/v1_1/{_settings.CloudName}/video/upload",
                ApiKey: _settings.ApiKey,
                Signature: signature,
                Timestamp: timestamp,
                Folder: _settings.FolderName,
                PublicId: publicId, // <--- You will need to add this property to your record definition!
                MaxFileSize: _settings.MaxVideoSizeMB * 1024 * 1024L,
                AllowedFormats: _settings.AllowedFormats
            );

            _logger.LogInformation("Generated signed upload credentials for lesson {LessonId} by user {UserId}", lessonId, userId);
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signed upload credentials for lesson {LessonId}", lessonId);
            return Result.Failure<SignedVideoUploadResponse>(
                new Error("Video.SignatureError", "Failed to generate upload credentials.", StatusCodes.Status500InternalServerError));
        }
    }

    public async Task<Result<FinalizeVideoUploadResponse>> FinalizeUploadAsync(
        int lessonId,
        FinalizeVideoUploadRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Verify lesson exists and user has permission
        var lesson = await _context.Lessons
            .Include(l => l.Section).ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure<FinalizeVideoUploadResponse>(LessonErrors.NotFound);

        // Check authorization: user must be course owner or superadmin
        var isAuthorized = await IsUserAuthorizedAsync(lessonId, userId, cancellationToken);
        if (!isAuthorized)
            return Result.Failure<FinalizeVideoUploadResponse>(LessonErrors.UnauthorizedModification);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.PublicId) || string.IsNullOrWhiteSpace(request.VideoUrl))
            return Result.Failure<FinalizeVideoUploadResponse>(
                new Error("Video.InvalidData", "Public ID and video URL are required.", StatusCodes.Status400BadRequest));

        if (request.DurationSeconds <= 0)
            return Result.Failure<FinalizeVideoUploadResponse>(
                new Error("Video.InvalidDuration", "Duration must be greater than 0.", StatusCodes.Status400BadRequest));

        // Update lesson with new video
        lesson.CloudinaryPublicId = request.PublicId;
        lesson.CloudinaryVideoUrl = request.VideoUrl;
        lesson.Duration = TimeSpan.FromSeconds(request.DurationSeconds);
        lesson.UpdatedOn = DateTime.UtcNow;
        lesson.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Finalized video upload for lesson {LessonId} with public ID {PublicId}", lessonId, request.PublicId);

        var response = new FinalizeVideoUploadResponse(
            LessonId: lessonId,
            PublicId: request.PublicId,
            VideoUrl: request.VideoUrl,
            Duration: TimeSpan.FromSeconds(request.DurationSeconds)
        );

        return Result.Success(response);
    }



    public async Task<Result<VideoLinkResponse>> GetVideoLinkAsync(
        int lessonId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Fetch lesson with course info
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section)
            .ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        // Validate lesson exists
        if (lesson is null)
            return Result.Failure<VideoLinkResponse>(LessonErrors.NotFound);

        // Validate video exists
        if (string.IsNullOrWhiteSpace(lesson.CloudinaryVideoUrl))
            return Result.Failure<VideoLinkResponse>(LessonErrors.VideoNotFound);

        // Validate lesson is published
        if (!lesson.IsPublished)
            return Result.Failure<VideoLinkResponse>(LessonErrors.NotFound);

        // Check privacy and access
        var isInstructor = await IsUserAuthorizedAsync(lessonId, userId, cancellationToken);

        // If video is private, only instructor can access
        if (lesson.IsVideoPrivate && isInstructor)
            return Result.Failure<VideoLinkResponse>(LessonErrors.UnauthorizedModification);

        //TODO Change If video is private, only instructor can access

        // For public videos, student must be enrolled (preview lesson or purchased course)
        if (!isInstructor && !lesson.IsPreview)
        {
            var isEnrolled = await _context.CourseUsers
                .AnyAsync(cu => cu.CourseId == lesson.Section.CourseId && cu.UserId == userId, cancellationToken);

            if (!isEnrolled)
                return Result.Failure<VideoLinkResponse>(LessonErrors.NotEnrolled);
        }

        // Return video link
        return Result.Success(new VideoLinkResponse(
            LessonId: lesson.Id,
            VideoUrl: lesson.CloudinaryVideoUrl,
            DurationSeconds: lesson.Duration.TotalSeconds,
            IsVideoPrivate: lesson.IsVideoPrivate
        ));
    }

    public async Task<Result> DeleteVideoAsync(
        int lessonId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // TODO Why You Use .Include(l => l.Section).ThenInclude(s => s.Course)?
        var lesson = await _context.Lessons
            .Include(l => l.Section).ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        // Check authorization: user must be course owner or superadmin
        var isAuthorized = await IsUserAuthorizedAsync(lessonId, userId, cancellationToken);
        if (!isAuthorized)
            return Result.Failure(LessonErrors.UnauthorizedModification);

        if (string.IsNullOrWhiteSpace(lesson.CloudinaryPublicId))
            return Result.Failure(
                new Error("Video.NotAttached", "No video is attached to this lesson.", StatusCodes.Status404NotFound));

        try
        {
            var deleteParams = new DeletionParams(lesson.CloudinaryPublicId)
            {
                ResourceType = ResourceType.Video
            };
            await _cloudinary.DestroyAsync(deleteParams);

            // Clear video from lesson
            lesson.CloudinaryPublicId = null;
            lesson.CloudinaryVideoUrl = null;
            lesson.Duration = TimeSpan.Zero;
            lesson.UpdatedOn = DateTime.UtcNow;
            lesson.UpdatedById = userId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted video {PublicId} from lesson {LessonId}", lesson.CloudinaryPublicId, lessonId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting video {PublicId} from lesson {LessonId}", lesson.CloudinaryPublicId, lessonId);
            return Result.Failure(
                new Error("Video.DeleteError", "Failed to delete video from Cloudinary.", StatusCodes.Status500InternalServerError));
        }
    }

    private async Task<bool> IsUserAuthorizedAsync(int lessonId, string userId, CancellationToken cancellationToken)
    {
        // Check if user is SuperAdmin
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null && await _userManager.IsInRoleAsync(user, DefaultRoles.SuperAdmin))
            return true;
        if (user is not null && await _userManager.IsInRoleAsync(user, DefaultRoles.Admin))
            return true;
        // Check if user is course owner
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section).ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        return lesson?.Section.Course.CreatedById == userId;
    }
}

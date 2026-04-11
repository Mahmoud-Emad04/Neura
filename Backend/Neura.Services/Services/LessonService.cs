using Neura.Core.Contracts.Lessons;
using Neura.Core.Enums;

namespace Neura.Services.Services;

public class LessonService(
    ApplicationDbContext context,
    ICloudinaryService cloudinaryService) : ILessonService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICloudinaryService _cloudinaryService = cloudinaryService;

    public async Task<Result<int>> CreateLessonMetadataAsync(CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var validSection = await _context.Sections.AnyAsync(s => s.Id == request.SectionId, cancellationToken);

        if (!validSection)
            return Result.Failure<int>(SectionErrors.SectionNotFound);

        var lastOrder = await _context.Lessons
            .Where(l => l.SectionId == request.SectionId)
            .MaxAsync(l => (int?)l.OrderIndex, cancellationToken) ?? 0;

        var lesson = new Lesson
        {
            Title = request.Title,
            SectionId = request.SectionId,
            Type = request.Type,
            OrderIndex = lastOrder + 1,
            IsPublished = false
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(lesson.Id);
    }

    public async Task<Result> CompleteLessonAsync(int id, CompleteLessonRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await _context.Lessons.FindAsync(id, cancellationToken) is not { } lesson)
            return Result.Failure(LessonErrors.NotFound);

        // Handle Cloudinary video upload only
        if (request.VideoFile is not null && lesson.Type == LessonType.Video)
        {
            var uploadResult = await _cloudinaryService.UploadVideoAsync(
                request.VideoFile,
                id,
                true,  // Always upload as private
                cancellationToken);

            if (uploadResult.IsFailure)
                return Result.Failure(uploadResult.Error);

            lesson.CloudinaryVideoUrl = uploadResult.Value;
            lesson.IsVideoPrivate = true;  // Always set to private
            lesson.CloudinaryPublicId = ExtractPublicIdFromUrl(uploadResult.Value);
        }

        lesson.Description = request.Description;
        lesson.IsPreview = request.IsPreview;
        lesson.IsPublished = true;

        if (request.ScheduledDate.HasValue)
            lesson.ScheduledDate = request.ScheduledDate.Value;
        else
            lesson.ScheduledDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<LessonResponse>> GetLessonByIdAsync(int lessonId, string userId, CancellationToken ct)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null) return Result.Failure<LessonResponse>(LessonErrors.NotFound);

        var isInstructor = lesson.Section.Course.CreatedById == userId;
        var canView = isInstructor || lesson.IsPreview;

        if (!canView) return Result.Failure<LessonResponse>(LessonErrors.NotEnrolled);

        var response = lesson.Adapt<LessonResponse>();
        return Result.Success(response);
    }


    /// <summary>
    /// Gets the Cloudinary video URL for streaming a lesson's video.
    /// For all videos, generates a signed URL valid for 1 hour that can only be streamed, not downloaded.
    /// The URL includes authentication tokens to prevent direct access.
    /// </summary>
    public async Task<Result<CloudinaryVideoResponse>> GetCloudinaryVideoAsync(
        int lessonId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure<CloudinaryVideoResponse>(LessonErrors.NotFound);

        // Check if video exists in Cloudinary
        if (string.IsNullOrEmpty(lesson.CloudinaryVideoUrl))
            return Result.Failure<CloudinaryVideoResponse>(LessonErrors.VideoNotFound);

        var isInstructor = lesson.Section.Course.CreatedById == userId;

        // Check access based on video privacy settings
        if (lesson.IsVideoPrivate && !isInstructor)
        {
            // Private videos: check enrollment
            var isEnrolled = await _context.CourseUsers
                .AnyAsync(cu =>
                    cu.UserId == userId &&
                    cu.CourseId == lesson.Section.CourseId &&
                    !cu.IsDeleted,
                    cancellationToken);

            if (!isEnrolled)
                return Result.Failure<CloudinaryVideoResponse>(
                    new Error("Lesson.NotEnrolled", "You must be enrolled in this course to access this video", StatusCodes.Status403Forbidden));
        }

        // Generate signed URL for streaming (valid for 1 hour)
        var signedUrl = _cloudinaryService.GenerateSignedUrl(lesson.CloudinaryVideoUrl, 3600);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        return Result.Success(new CloudinaryVideoResponse(
            Url: signedUrl,
            SignedUrl: signedUrl,
            IsPrivate: lesson.IsVideoPrivate,
            IsPreview: lesson.IsPreview,
            Duration: (int)lesson.Duration.TotalSeconds,
            ExpiresAt: expiresAt
        ));
    }

    /// <summary>
    /// Extracts the Cloudinary public ID from a Cloudinary URL.
    /// </summary>
    private static string ExtractPublicIdFromUrl(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            var uri = new Uri(url);
            var segments = uri.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var lessonIndex = Array.IndexOf(segments, "lessons");
            if (lessonIndex >= 0 && lessonIndex + 2 < segments.Length)
            {
                var lessonId = segments[lessonIndex + 1];
                var fileName = segments[lessonIndex + 2];
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                return $"lessons/{lessonId}/{fileNameWithoutExtension}";
            }

            return url;
        }
        catch
        {
            return url;
        }
    }

    /// <summary>
    /// Updates the position of a lesson within its section.
    /// </summary>
    public async Task<Result> UpdateLessonPositionAsync(int lessonId, int newPosition, string userId,
        CancellationToken cancellationToken = default)
    {
        // Get the lesson with its section and course
        var lesson = await _context.Lessons
            .Include(l => l.Section.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        // Verify authorization (instructor only)
        if (lesson.Section.Course.CreatedById != userId)
            return Result.Failure(LessonErrors.UnauthorizedModification);

        // Get all lessons in the section, ordered by position
        var lessonsInSection = await _context.Lessons
            .Where(l => l.SectionId == lesson.SectionId)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync(cancellationToken);

        // Validate new position
        if (newPosition < 1 || newPosition > lessonsInSection.Count)
            return Result.Failure(LessonErrors.PositionOutOfRange);

        int oldPosition = lesson.OrderIndex;

        // If moving to same position, no change needed
        if (oldPosition == newPosition)
            return Result.Success();

        // Reorder lessons
        if (oldPosition < newPosition)
        {
            // Moving down: shift lessons up
            foreach (var l in lessonsInSection.Where(l => l.OrderIndex > oldPosition && l.OrderIndex <= newPosition))
            {
                l.OrderIndex--;
            }
        }
        else
        {
            // Moving up: shift lessons down
            foreach (var l in lessonsInSection.Where(l => l.OrderIndex >= newPosition && l.OrderIndex < oldPosition))
            {
                l.OrderIndex++;
            }
        }

        lesson.OrderIndex = newPosition;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Updates the privacy status of a lesson.
    /// Note: Videos are ALWAYS private for security. Only IsPreview can be changed.
    /// </summary>
    public async Task<Result> UpdateLessonPrivacyAsync(int lessonId, UpdateLessonPrivacyRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        // Verify authorization (instructor only)
        if (lesson.Section.Course.CreatedById != userId)
            return Result.Failure(LessonErrors.UnauthorizedModification);

        // Update lesson preview setting only
        lesson.IsPreview = request.IsPreview;

        // IsVideoPrivate is ALWAYS true (videos always private for security)
        lesson.IsVideoPrivate = true;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Updates basic lesson information.
    /// </summary>
    public async Task<Result> UpdateLessonAsync(int lessonId, UpdateLessonRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        // Verify authorization (instructor only)
        if (lesson.Section.Course.CreatedById != userId)
            return Result.Failure(LessonErrors.UnauthorizedModification);

        // Update fields
        if (!string.IsNullOrEmpty(request.Title))
            lesson.Title = request.Title;

        if (request.Description is not null)
            lesson.Description = request.Description;

        lesson.IsPreview = request.IsPreview;

        if (request.ScheduledDate.HasValue)
            lesson.ScheduledDate = request.ScheduledDate.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Deletes a lesson and adjusts positions of remaining lessons.
    /// </summary>
    public async Task<Result> DeleteLessonAsync(int lessonId, string userId,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        // Verify authorization (instructor only)
        if (lesson.Section.Course.CreatedById != userId)
            return Result.Failure(LessonErrors.UnauthorizedModification);

        int deletedPosition = lesson.OrderIndex;
        int sectionId = lesson.SectionId;

        // Delete the lesson
        _context.Lessons.Remove(lesson);

        // Delete associated video from Cloudinary if exists
        if (!string.IsNullOrEmpty(lesson.CloudinaryPublicId))
        {
            await _cloudinaryService.DeleteVideoAsync(lesson.CloudinaryPublicId, cancellationToken);
        }

        // Reorder remaining lessons in the section
        var lessonsAfter = await _context.Lessons
            .Where(l => l.SectionId == sectionId && l.OrderIndex > deletedPosition)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync(cancellationToken);

        foreach (var l in lessonsAfter)
        {
            l.OrderIndex--;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Gets all lessons in a section with their position information.
    /// </summary>
    public async Task<Result<List<LessonWithPositionResponse>>> GetSectionLessonsAsync(int sectionId, string userId,
        CancellationToken cancellationToken = default)
    {
        var section = await _context.Sections
            .Include(s => s.Lessons)
            .FirstOrDefaultAsync(s => s.Id == sectionId, cancellationToken);

        if (section is null)
            return Result.Failure<List<LessonWithPositionResponse>>(SectionErrors.SectionNotFound);

        var totalLessons = section.Lessons.Count;
        var lessons = section.Lessons
            .OrderBy(l => l.OrderIndex)
            .Select(l => new LessonWithPositionResponse(
                Id: l.Id,
                Title: l.Title,
                Description: l.Description,
                Position: l.OrderIndex,
                TotalInSection: totalLessons,
                IsPreview: l.IsPreview,
                IsVideoPrivate: l.IsVideoPrivate,
                IsPublished: l.IsPublished,
                Type: l.Type,
                VideoUrl: l.CloudinaryVideoUrl,
                CreatedAt: l.UpdatedOn ?? l.CreatedOn
            ))
            .ToList();

        return Result.Success(lessons);
    }
}

using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Enums;
using NReco.VideoInfo;

namespace Neura.Services.Services;

public class LessonService(
    ApplicationDbContext context,
    ICloudinaryService cloudinaryService) : ILessonService
{
    private readonly ICloudinaryService _cloudinaryService = cloudinaryService;
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<int>> CreateLessonMetadataAsync(CreateLessonRequest request, string userId,
        CancellationToken cancellationToken)
    {
        var section = await _context.Sections
            .AsNoTracking()
            .Select(s => new { s.Id, s.CourseId })
            .FirstOrDefaultAsync(s => s.Id == request.SectionId, cancellationToken);

        if (section is null)
            return Result.Failure<int>(SectionErrors.SectionNotFound);

        //TODO Change this to RPC

        #region Change this to RPC

        var ownerMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner];
        var coInstructorMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CoInstructor];

        var hasEditPermission = await _context.CourseUsers
            .AnyAsync(cu => cu.CourseId == section.CourseId
                            && cu.UserId == userId
                            && (cu.PermissionsMask == ownerMask || cu.PermissionsMask == coInstructorMask),
                cancellationToken);

        if (!hasEditPermission)
            return Result.Failure<int>(CourseErrors.UnauthorizedAction);

        #endregion

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

        if (request.VideoFile is not null && lesson.Type == LessonType.Video)
        {
            const long maxFileSize = 500 * 1024 * 1024;
            if (request.VideoFile.Length > maxFileSize)
                return Result.Failure(LessonErrors.VideoTooLarge);

            try
            {
                var ffProbe = new FFProbe();
                var tempFile = Path.GetTempFileName();
                try
                {
                    await using (var stream = new FileStream(tempFile, FileMode.Create))
                    {
                        await request.VideoFile.CopyToAsync(stream, cancellationToken);
                    }

                    var mediaInfo = ffProbe.GetMediaInfo(tempFile);

                    if (mediaInfo.Duration > TimeSpan.FromMinutes(20))
                        return Result.Failure(LessonErrors.VideoTooLong);

                    lesson.Duration = mediaInfo.Duration;
                }
                finally
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
            catch (Exception)
            {
                return Result.Failure(LessonErrors.InvalidVideo);
            }

            var uploadResult = await _cloudinaryService.UploadVideoAsync(
                request.VideoFile,
                id,
                true,
                cancellationToken);

            if (uploadResult.IsFailure)
                return Result.Failure(uploadResult.Error);

            lesson.CloudinaryVideoUrl = uploadResult.Value;
            lesson.IsVideoPrivate = true;
            lesson.CloudinaryPublicId = ExtractPublicIdFromUrl(uploadResult.Value);
        }

        lesson.Description = request.Description;
        lesson.IsPreview = request.IsPreview;
        lesson.IsPublished = true;
        lesson.ScheduledDate = request.ScheduledDate ?? DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<LessonResponse>> GetLessonByIdAsync(int lessonId, string userId, CancellationToken ct)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section).ThenInclude(section => section.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null) return Result.Failure<LessonResponse>(LessonErrors.NotFound);

        var isInstructor = lesson.Section.Course.CreatedById == userId;
        var canView = isInstructor || lesson.IsPreview;

        if (!canView) return Result.Failure<LessonResponse>(LessonErrors.NotEnrolled);

        var response = lesson.Adapt<LessonResponse>();
        return Result.Success(response);
    }

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

        if (string.IsNullOrEmpty(lesson.CloudinaryPublicId))
            return Result.Failure<CloudinaryVideoResponse>(LessonErrors.VideoNotFound);

        var isInstructor = lesson.Section.Course.CreatedById == userId;
        var isFreePreview = lesson.IsPreview;

        if (!isInstructor && !isFreePreview)
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
                    new Error("Lesson.NotEnrolled", "You must be enrolled in this course to access this video",
                        StatusCodes.Status403Forbidden));
        }

        var signedUrl = _cloudinaryService.GenerateSignedUrl(lesson.CloudinaryPublicId);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        return Result.Success(new CloudinaryVideoResponse(
            signedUrl,
            signedUrl,
            lesson.IsVideoPrivate,
            lesson.IsPreview,
            (int)lesson.Duration.TotalSeconds,
            expiresAt
        ));
    }

    public async Task<Result> UpdateLessonPositionAsync(int lessonId, int newPosition, string userId,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);
/*
        var ownerMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner];
        var coInstructorMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CoInstructor];

        var hasEditPermission = await _context.CourseUsers
            .AnyAsync(cu => cu.CourseId == lesson.Section.CourseId
                            && cu.UserId == userId
                            && (cu.PermissionsMask == ownerMask || cu.PermissionsMask == coInstructorMask),
                cancellationToken);

        if (!hasEditPermission)
            return Result.Failure(LessonErrors.UnauthorizedModification);
*/
        var oldPosition = lesson.OrderIndex;
        if (oldPosition == newPosition)
            return Result.Success();

        var totalLessons = await _context.Lessons
            .CountAsync(l => l.SectionId == lesson.SectionId, cancellationToken);

        if (newPosition < 1 || newPosition > totalLessons)
            return Result.Failure(LessonErrors.PositionOutOfRange);

        var minPosition = Math.Min(oldPosition, newPosition);
        var maxPosition = Math.Max(oldPosition, newPosition);

        var affectedLessons = await _context.Lessons
            .Where(l => l.SectionId == lesson.SectionId
                        && l.OrderIndex >= minPosition
                        && l.OrderIndex <= maxPosition
                        && l.Id != lessonId)
            .ToListAsync(cancellationToken);

        if (oldPosition < newPosition)
        {
            foreach (var l in affectedLessons)
                l.OrderIndex--;
        }
        else
        {
            foreach (var l in affectedLessons)
                l.OrderIndex++;
        }

        lesson.OrderIndex = newPosition;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateLessonPrivacyAsync(int lessonId, UpdateLessonPrivacyRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);
/*
        var ownerMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner];
        var coInstructorMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CoInstructor];

        var hasEditPermission = await _context.CourseUsers
            .AnyAsync(cu => cu.CourseId == lesson.Section.CourseId
                            && cu.UserId == userId
                            && (cu.PermissionsMask == ownerMask || cu.PermissionsMask == coInstructorMask),
                cancellationToken);

        if (!hasEditPermission)
            return Result.Failure(LessonErrors.UnauthorizedModification);
*/
        lesson.IsPreview = request.IsPreview;
        if (lesson.Type == LessonType.Video)
        {
            lesson.IsVideoPrivate = request.IsVideoPrivate;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateLessonAsync(int lessonId, UpdateLessonRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);
/*
        var ownerMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner];
        var coInstructorMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CoInstructor];

        var hasEditPermission = await _context.CourseUsers
            .AnyAsync(cu => cu.CourseId == lesson.Section.CourseId
                            && cu.UserId == userId
                            && (cu.PermissionsMask == ownerMask || cu.PermissionsMask == coInstructorMask),
                cancellationToken);

        if (!hasEditPermission)
            return Result.Failure(LessonErrors.UnauthorizedModification);
*/
        if (!string.IsNullOrWhiteSpace(request.Title))
            lesson.Title = request.Title;

        lesson.Description = request.Description;

        lesson.IsPreview = request.IsPreview;

        if (request.ScheduledDate.HasValue)
            lesson.ScheduledDate = request.ScheduledDate.Value;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    ///     Deletes a lesson and adjusts positions of remaining lessons.
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

        var deletedPosition = lesson.OrderIndex;
        var sectionId = lesson.SectionId;

        // Delete the lesson
        _context.Lessons.Remove(lesson);

        // Delete associated video from Cloudinary if exists
        if (!string.IsNullOrEmpty(lesson.CloudinaryPublicId))
            await _cloudinaryService.DeleteVideoAsync(lesson.CloudinaryPublicId, cancellationToken);

        // Reorder remaining lessons in the section
        var lessonsAfter = await _context.Lessons
            .Where(l => l.SectionId == sectionId && l.OrderIndex > deletedPosition)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync(cancellationToken);

        foreach (var l in lessonsAfter) l.OrderIndex--;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    ///     Gets all lessons in a section with their position information.
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
                l.Id,
                l.Title,
                l.Description,
                l.OrderIndex,
                totalLessons,
                l.IsPreview,
                l.IsVideoPrivate,
                l.IsPublished,
                l.Type,
                l.CloudinaryVideoUrl,
                l.UpdatedOn ?? l.CreatedOn
            ))
            .ToList();

        return Result.Success(lessons);
    }

    /// <summary>
    ///     Extracts the Cloudinary public ID from a Cloudinary URL.
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
}
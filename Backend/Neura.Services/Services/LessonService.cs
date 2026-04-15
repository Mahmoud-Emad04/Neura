using Ganss.Xss;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Enums;

namespace Neura.Services.Services;

public class LessonService(
    ApplicationDbContext context) : ILessonService
{
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

        //var ownerMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner];
        //var coInstructorMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CoInstructor];

        //var hasEditPermission = await _context.CourseUsers
        //    .AnyAsync(cu => cu.CourseId == section.CourseId
        //                    && cu.UserId == userId
        //                    && (cu.PermissionsMask == ownerMask || cu.PermissionsMask == coInstructorMask),
        //        cancellationToken);

        //if (!hasEditPermission)
        //    return Result.Failure<int>(CourseErrors.UnauthorizedAction);

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
            foreach (var l in affectedLessons)
                l.OrderIndex--;
        else
            foreach (var l in affectedLessons)
                l.OrderIndex++;

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
        if (lesson.Type == LessonType.Video) lesson.IsVideoPrivate = request.IsVideoPrivate;

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

    public async Task<Result> UpdateArticleContentAsync(int lessonId, UpdateArticleRequest request, string userId,
        CancellationToken ct = default)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        if (lesson.Type != LessonType.Article)
            return Result.Failure(new Error("Lesson.InvalidType", "Content can only be added to Article-type lessons.",
                StatusCodes.Status400BadRequest));

        var sanitizer = new HtmlSanitizer();
        var cleanHtml = sanitizer.Sanitize(request.HtmlContent);

        lesson.ArticleContent = cleanHtml;
        lesson.IsPublished = true;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<ArticleResponse>> GetArticleContentAsync(int lessonId, string userId,
        CancellationToken ct = default)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null)
            return Result.Failure<ArticleResponse>(LessonErrors.NotFound);

        if (lesson.Type != LessonType.Article)
            return Result.Failure<ArticleResponse>(
                new Error("Lesson.InvalidType", "This lesson is not an article.", StatusCodes.Status400BadRequest));

        //bool canView = lesson.IsPreview;

        //if (!canView)
        //{
        //    var hasAccess = await _context.CourseUsers
        //        .AnyAsync(cu => cu.CourseId == lesson.Section.CourseId && cu.UserId == userId, ct);

        //    if (!hasAccess)
        //        return Result.Failure<ArticleResponse>(LessonErrors.NotEnrolled);
        //}

        var response = new ArticleResponse(
            lesson.Id,
            lesson.Title,
            lesson.ArticleContent ?? string.Empty
        );

        return Result.Success(response);
    }
}
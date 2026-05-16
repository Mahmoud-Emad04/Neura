
using Neura.Core.Contracts.Lessons;
using Neura.Core.Enums;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class LessonProgressService(
    ApplicationDbContext context,
    IServiceHelpers helpers,
    ILogger<LessonProgressService> logger) : ILessonProgressService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IServiceHelpers _helpers = helpers;
    private readonly ILogger<LessonProgressService> _logger = logger;

    // ══════════════════════════════════════════════════════════════
    // Mark Lesson Completed (called by frontend for Video / Article)
    // ══════════════════════════════════════════════════════════════
    public async Task<Result<LessonCompletionResponse>> MarkLessonCompletedAsync(
        int lessonId, string userId, CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted, cancellationToken);

        if (lesson is null)
            return Result.Failure<LessonCompletionResponse>(LessonProgressErrors.LessonNotFound);

        if (!lesson.IsPublished)
            return Result.Failure<LessonCompletionResponse>(LessonProgressErrors.LessonNotAccessible);

        // Access guard: enrolled OR preview lesson
        var isEnrolled = await _context.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu =>
                cu.CourseId == lesson.Section.CourseId &&
                cu.UserId == userId &&
                !cu.IsDeleted, cancellationToken);

        if (!isEnrolled && !lesson.IsPreview)
            return Result.Failure<LessonCompletionResponse>(LessonProgressErrors.NotEnrolled);

        // Idempotent: if already completed, return existing record
        var existing = await _context.LessonCompletions
            .FirstOrDefaultAsync(lc => lc.LessonId == lessonId && lc.UserId == userId, cancellationToken);

        if (existing is not null)
            return Result.Success(new LessonCompletionResponse(lessonId, true, existing.CompletedOn));

        var completion = new LessonCompletion
        {
            UserId = userId,
            LessonId = lessonId,
            CompletedOn = DateTime.UtcNow
        };

        await _context.LessonCompletions.AddAsync(completion, cancellationToken);

        // Bonus: update CourseUser.LastAccessedOn (only if enrolled)
        if (isEnrolled)
        {
            var courseUser = await _context.CourseUsers
                .FirstOrDefaultAsync(cu =>
                    cu.CourseId == lesson.Section.CourseId &&
                    cu.UserId == userId &&
                    !cu.IsDeleted, cancellationToken);

            if (courseUser is not null)
                courseUser.LastAccessedOn = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} completed lesson {LessonId} ('{Title}')",
            userId, lessonId, lesson.Title);

        return Result.Success(new LessonCompletionResponse(lessonId, true, completion.CompletedOn));
    }

    // ══════════════════════════════════════════════════════════════
    // Quiz Auto-Completion Hook (called from ExamService on pass)
    // ══════════════════════════════════════════════════════════════
    public async Task MarkQuizLessonCompletedAsync(
        int lessonId, string userId, CancellationToken cancellationToken = default)
    {
        var alreadyCompleted = await _context.LessonCompletions
            .AnyAsync(lc => lc.LessonId == lessonId && lc.UserId == userId, cancellationToken);

        // Pass-once-completed-forever: never overwrite, never remove
        if (alreadyCompleted)
            return;

        await _context.LessonCompletions.AddAsync(new LessonCompletion
        {
            UserId = userId,
            LessonId = lessonId,
            CompletedOn = DateTime.UtcNow
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} auto-completed quiz lesson {LessonId} via passing exam",
            userId, lessonId);
    }

    // ══════════════════════════════════════════════════════════════
    // Get Course Progress
    // ══════════════════════════════════════════════════════════════
    public async Task<Result<CourseProgressResponse>> GetCourseProgressAsync(
        string courseKeyId, string userId, CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(courseKeyId, out var courseId))
            return Result.Failure<CourseProgressResponse>(LessonProgressErrors.CourseNotFound);

        var lessons = await GetOrderedAccessibleLessonsAsync(courseId, userId, cancellationToken);

        if (lessons.Count == 0)
        {
            return Result.Success(new CourseProgressResponse(
                courseKeyId, 0, 0, 0, false, null, []));
        }

        var lessonIds = lessons.Select(l => l.Id).ToList();

        var completedIds = await _context.LessonCompletions
            .AsNoTracking()
            .Where(lc => lc.UserId == userId && lessonIds.Contains(lc.LessonId))
            .Select(lc => lc.LessonId)
            .ToHashSetAsync(cancellationToken);

        var completedCount = completedIds.Count;
        var totalCount = lessons.Count;
        var percentage = (int)Math.Round((double)completedCount / totalCount * 100);
        var isCourseCompleted = completedCount == totalCount;

        // Find next lesson = first lesson in order that is NOT completed
        NextLessonResponse? next = null;
        if (!isCourseCompleted)
        {
            var nextLesson = lessons.First(l => !completedIds.Contains(l.Id));
            next = new NextLessonResponse(
                nextLesson.Id,
                nextLesson.SectionId,
                nextLesson.Title,
                nextLesson.Type.ToString(),
                nextLesson.OrderIndex);
        }

        return Result.Success(new CourseProgressResponse(
            courseKeyId,
            totalCount,
            completedCount,
            percentage,
            isCourseCompleted,
            next,
            completedIds.ToList()));
    }

    // ══════════════════════════════════════════════════════════════
    // Get Next Lesson (shortcut)
    // ══════════════════════════════════════════════════════════════
    public async Task<Result<NextLessonResponse?>> GetNextLessonAsync(
        string courseKeyId, string userId, CancellationToken cancellationToken = default)
    {
        var progressResult = await GetCourseProgressAsync(courseKeyId, userId, cancellationToken);
        if (progressResult.IsFailure)
            return Result.Failure<NextLessonResponse?>(progressResult.Error);

        return Result.Success(progressResult.Value.NextLesson);
    }

    // ══════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns lessons ordered by Section.Position then Lesson.OrderIndex.
    /// Filters out deleted/unpublished. Filters preview-only if user not enrolled.
    /// </summary>
    private async Task<List<LessonOrderInfo>> GetOrderedAccessibleLessonsAsync(
        int courseId, string userId, CancellationToken cancellationToken)
    {
        var isEnrolled = await _context.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu => cu.CourseId == courseId && cu.UserId == userId && !cu.IsDeleted,
                cancellationToken);

        var query = _context.Lessons
            .AsNoTracking()
            .Where(l =>
                l.Section.CourseId == courseId &&
                !l.IsDeleted &&
                !l.Section.IsDeleted &&
                l.IsPublished);

        if (!isEnrolled)
            query = query.Where(l => l.IsPreview);

        return await query
            .OrderBy(l => l.Section.Position)
            .ThenBy(l => l.OrderIndex)
            .Select(l => new LessonOrderInfo(
                l.Id, l.SectionId, l.Title, l.Type, l.OrderIndex))
            .ToListAsync(cancellationToken);
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = _helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }
        courseId = numbers[0];
        return true;
    }

    private record LessonOrderInfo(int Id, int SectionId, string Title, LessonType Type, int OrderIndex);
}
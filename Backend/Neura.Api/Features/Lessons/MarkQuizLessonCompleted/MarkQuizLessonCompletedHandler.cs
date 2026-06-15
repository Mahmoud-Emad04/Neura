using MediatR;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.MarkQuizLessonCompleted;

internal sealed class MarkQuizLessonCompletedHandler(
    ApplicationDbContext context,
    ILogger<MarkQuizLessonCompletedHandler> logger)
    : IRequestHandler<MarkQuizLessonCompletedCommand, Result<LessonCompletionResponse>>
{
    public async Task<Result<LessonCompletionResponse>> Handle(
        MarkQuizLessonCompletedCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var userId = command.UserId;

        var lesson = await context.Lessons
            .AsNoTracking()
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted, ct);

        if (lesson is null)
            return Result.Failure<LessonCompletionResponse>(LessonProgressErrors.LessonNotFound);

        if (!lesson.IsPublished)
            return Result.Failure<LessonCompletionResponse>(LessonProgressErrors.LessonNotAccessible);

        var isEnrolled = await context.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu =>
                cu.CourseId == lesson.Section.CourseId &&
                cu.UserId == userId &&
                !cu.IsDeleted, ct);

        if (!isEnrolled && !lesson.IsPreview)
            return Result.Failure<LessonCompletionResponse>(LessonProgressErrors.NotEnrolled);

        var existing = await context.LessonCompletions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(lc => lc.LessonId == lessonId && lc.UserId == userId, ct);

        bool isCompletedNow;
        DateTime? completedOn = null;

        if (existing is not null)
        {
            existing.IsDeleted = !existing.IsDeleted;
            existing.CompletedOn = DateTime.UtcNow;
            isCompletedNow = !existing.IsDeleted;
            completedOn = isCompletedNow ? existing.CompletedOn : null;

            logger.LogInformation(
                "User {UserId} {Action} auto-completion for quiz lesson {LessonId} ('{Title}')",
                userId, isCompletedNow ? "completed" : "unmarked", lessonId, lesson.Title);
        }
        else
        {
            var completion = new LessonCompletion
            {
                UserId = userId,
                LessonId = lessonId,
                CompletedOn = DateTime.UtcNow,
                IsDeleted = false
            };

            await context.LessonCompletions.AddAsync(completion, ct);
            isCompletedNow = true;
            completedOn = completion.CompletedOn;

            logger.LogInformation(
                "User {UserId} auto-completed quiz lesson {LessonId} ('{Title}')",
                userId, lessonId, lesson.Title);
        }

        if (isEnrolled)
        {
            var courseUser = await context.CourseUsers
                .FirstOrDefaultAsync(cu =>
                    cu.CourseId == lesson.Section.CourseId &&
                    cu.UserId == userId &&
                    !cu.IsDeleted, ct);

            if (courseUser is not null)
                courseUser.LastAccessedOn = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);

        return Result.Success(new LessonCompletionResponse(lessonId, isCompletedNow, completedOn));
    }
}

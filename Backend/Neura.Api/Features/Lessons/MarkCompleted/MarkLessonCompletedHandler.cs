using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Entities;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.MarkCompleted;

internal sealed class MarkLessonCompletedHandler(
    ApplicationDbContext context,
    ILogger<MarkLessonCompletedHandler> logger) 
    : IRequestHandler<MarkLessonCompletedCommand, Result<LessonCompletionResponse>>
{
    public async Task<Result<LessonCompletionResponse>> Handle(
        MarkLessonCompletedCommand command, CancellationToken ct)
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

        var existing = await context.LessonCompletions
            .FirstOrDefaultAsync(lc => lc.LessonId == lessonId && lc.UserId == userId, ct);

        if (existing is not null)
            return Result.Success(new LessonCompletionResponse(lessonId, true, existing.CompletedOn));

        var completion = new LessonCompletion
        {
            UserId = userId,
            LessonId = lessonId,
            CompletedOn = DateTime.UtcNow
        };

        await context.LessonCompletions.AddAsync(completion, ct);

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

        logger.LogInformation(
            "User {UserId} completed lesson {LessonId} ('{Title}')",
            userId, lessonId, lesson.Title);

        return Result.Success(new LessonCompletionResponse(lessonId, true, completion.CompletedOn));
    }
}

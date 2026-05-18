using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Entities;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.MarkQuizLessonCompleted;

internal sealed class MarkQuizLessonCompletedHandler(
    ApplicationDbContext context,
    ILogger<MarkQuizLessonCompletedHandler> logger) 
    : IRequestHandler<MarkQuizLessonCompletedCommand>
{
    public async Task Handle(
        MarkQuizLessonCompletedCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var userId = command.UserId;

        var alreadyCompleted = await context.LessonCompletions
            .AnyAsync(lc => lc.LessonId == lessonId && lc.UserId == userId, ct);

        if (alreadyCompleted)
            return;

        await context.LessonCompletions.AddAsync(new LessonCompletion
        {
            UserId = userId,
            LessonId = lessonId,
            CompletedOn = DateTime.UtcNow
        }, ct);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "User {UserId} auto-completed quiz lesson {LessonId} via passing exam",
            userId, lessonId);
    }
}

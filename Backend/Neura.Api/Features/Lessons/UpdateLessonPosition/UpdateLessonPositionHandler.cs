using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.UpdateLessonPosition;

internal sealed class UpdateLessonPositionHandler(
    ApplicationDbContext context) 
    : IRequestHandler<UpdateLessonPositionCommand, Result>
{
    public async Task<Result> Handle(
        UpdateLessonPositionCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var newPosition = command.Request.NewPosition;

        var lesson = await context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted, ct);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        var oldPosition = lesson.OrderIndex;
        if (oldPosition == newPosition)
            return Result.Success();

        var totalLessons = await context.Lessons
            .CountAsync(l => l.SectionId == lesson.SectionId, ct);

        if (newPosition < 1 || newPosition > totalLessons)
            return Result.Failure(LessonErrors.PositionOutOfRange);

        var minPosition = Math.Min(oldPosition, newPosition);
        var maxPosition = Math.Max(oldPosition, newPosition);

        var affectedLessons = await context.Lessons
            .Where(l => l.SectionId == lesson.SectionId
                        && l.OrderIndex >= minPosition
                        && l.OrderIndex <= maxPosition
                        && l.Id != lessonId)
            .ToListAsync(ct);

        if (oldPosition < newPosition)
            foreach (var l in affectedLessons)
                l.OrderIndex--;
        else
            foreach (var l in affectedLessons)
                l.OrderIndex++;

        lesson.OrderIndex = newPosition;
        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

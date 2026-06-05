using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.UpdateLesson;

internal sealed class UpdateLessonHandler(
    ApplicationDbContext context) 
    : IRequestHandler<UpdateLessonCommand, Result>
{
    public async Task<Result> Handle(
        UpdateLessonCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var request = command.Request;

        var lesson = await context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted, ct);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        if (!string.IsNullOrWhiteSpace(request.Title))
            lesson.Title = request.Title;

        lesson.Description = request.Description;
        lesson.IsPreview = request.IsPreview;

        if (request.ScheduledDate.HasValue)
            lesson.ScheduledDate = request.ScheduledDate.Value;

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

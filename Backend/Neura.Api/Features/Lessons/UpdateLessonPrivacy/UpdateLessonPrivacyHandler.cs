using MediatR;
using Neura.Api.Features.Exams.PublishExam;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.UpdateLessonPrivacy;

internal sealed class UpdateLessonPrivacyHandler(
    ApplicationDbContext context,
    ISender sender)
    : IRequestHandler<UpdateLessonPrivacyCommand, Result>
{
    public async Task<Result> Handle(
        UpdateLessonPrivacyCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var request = command.Request;

        var lesson = await context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted, ct);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        if (lesson.Type == LessonType.Quiz)
        {
            var result = await sender.Send(new PublishExamCommand(lessonId, command.UserId), ct);
            if (!result.IsSuccess)
                return Result.Failure(result.Error);
            return Result.Success();
        }

        lesson.IsPublished = !request.IsVideoPrivate;
        lesson.IsPreview = request.IsPreview;

        if (lesson.Type == LessonType.Video)
            lesson.IsVideoPrivate = request.IsVideoPrivate;

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}

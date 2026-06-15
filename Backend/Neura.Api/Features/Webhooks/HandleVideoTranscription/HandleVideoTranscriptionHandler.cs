using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Webhooks.HandleVideoTranscription;

internal sealed class HandleVideoTranscriptionHandler(
    ApplicationDbContext context,
    ILogger<HandleVideoTranscriptionHandler> logger)
    : IRequestHandler<HandleVideoTranscriptionCommand, Result>
{
    public async Task<Result> Handle(HandleVideoTranscriptionCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var lesson = await context.Lessons
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);

        if (lesson is null)
        {
            logger.LogWarning("Video transcription webhook received for non-existent lesson {LessonId}", request.LessonId);
            return Result.Failure(LessonErrors.NotFound);
        }

        if (lesson.VideoProcessingStatus != VideoProcessingStatus.Processing)
        {
            logger.LogWarning("Video transcription webhook received for lesson {LessonId} that is not in processing state. Current state: {State}", request.LessonId, lesson.VideoProcessingStatus);
        }

        lesson.SetVideoTranscription(request.VideoText);

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully saved video transcription for lesson {LessonId}", request.LessonId);

        return Result.Success();
    }
}

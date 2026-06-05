using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.DeleteLesson;

internal sealed class DeleteLessonHandler(
    ApplicationDbContext context,
    Cloudinary cloudinary,
    ILogger<DeleteLessonHandler> logger) 
    : IRequestHandler<DeleteLessonCommand, Result>
{
    public async Task<Result> Handle(
        DeleteLessonCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var userId = command.UserId;

        var lesson = await context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted, ct);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        await context.Lessons
            .Where(l => l.Id == lessonId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.IsDeleted, true), ct);

        if (lesson.Type == LessonType.Quiz)
        {
            await context.Exams.Where(l => l.LessonId == lessonId)
                .ExecuteUpdateAsync(s => s.SetProperty(ex => ex.IsDeleted, true), ct);
        }

        if (lesson.Type == LessonType.Video)
        {
            if (string.IsNullOrWhiteSpace(lesson.CloudinaryPublicId))
                return Result.Success();
                
            try
            {
                var deleteParams = new DeletionParams(lesson.CloudinaryPublicId)
                {
                    ResourceType = ResourceType.Video
                };
                await cloudinary.DestroyAsync(deleteParams);

                lesson.CloudinaryPublicId = null;
                lesson.CloudinaryVideoUrl = null;
                lesson.Duration = TimeSpan.Zero;
                lesson.UpdatedOn = DateTime.UtcNow;
                lesson.UpdatedById = userId;

                await context.SaveChangesAsync(ct);

                logger.LogInformation("Deleted video {PublicId} from lesson {LessonId}", lesson.CloudinaryPublicId, lessonId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting video {PublicId} from lesson {LessonId}", lesson.CloudinaryPublicId, lessonId);
                return Result.Failure(
                    new Neura.Core.Abstractions.Error("Video.DeleteError", "Failed to delete video from Cloudinary.", StatusCodes.Status500InternalServerError));
            }
        }
        
        return Result.Success();
    }
}

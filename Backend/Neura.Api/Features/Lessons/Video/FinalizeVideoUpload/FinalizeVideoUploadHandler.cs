using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.Video.FinalizeVideoUpload;

internal sealed class FinalizeVideoUploadHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<FinalizeVideoUploadHandler> logger) 
    : IRequestHandler<FinalizeVideoUploadCommand, Result<FinalizeVideoUploadResponse>>
{
    public async Task<Result<FinalizeVideoUploadResponse>> Handle(
        FinalizeVideoUploadCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var userId = command.UserId;
        var request = command.Request;

        var lesson = await context.Lessons
            .Include(l => l.Section).ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null)
            return Result.Failure<FinalizeVideoUploadResponse>(LessonErrors.NotFound);

        var isAuthorized = await IsUserAuthorizedAsync(lessonId, userId, ct);
        if (!isAuthorized)
            return Result.Failure<FinalizeVideoUploadResponse>(LessonErrors.UnauthorizedModification);

        if (string.IsNullOrWhiteSpace(request.PublicId) || string.IsNullOrWhiteSpace(request.VideoUrl))
            return Result.Failure<FinalizeVideoUploadResponse>(
                new Neura.Core.Abstractions.Error("Video.InvalidData", "Public ID and video URL are required.", StatusCodes.Status400BadRequest));

        if (request.DurationSeconds <= 0)
            return Result.Failure<FinalizeVideoUploadResponse>(
                new Neura.Core.Abstractions.Error("Video.InvalidDuration", "Duration must be greater than 0.", StatusCodes.Status400BadRequest));

        lesson.CloudinaryPublicId = request.PublicId;
        lesson.CloudinaryVideoUrl = request.VideoUrl;
        lesson.Duration = TimeSpan.FromSeconds(request.DurationSeconds);
        lesson.UpdatedOn = DateTime.UtcNow;
        lesson.UpdatedById = userId;
        lesson.Status = LessonStatus.Active;
        lesson.MarkVideoProcessing();

        await context.SaveChangesAsync(ct);

        Hangfire.BackgroundJob.Enqueue<Neura.Services.Jobs.NotifyVideoProcessorJob>(
            job => job.ExecuteAsync(lessonId, request.VideoUrl));

        logger.LogInformation("Finalized video upload for lesson {LessonId} with public ID {PublicId}", lessonId, request.PublicId);

        var response = new FinalizeVideoUploadResponse(
            LessonId: lessonId,
            PublicId: request.PublicId,
            VideoUrl: request.VideoUrl,
            Duration: TimeSpan.FromSeconds(request.DurationSeconds)
        );

        return Result.Success(response);
    }

    private async Task<bool> IsUserAuthorizedAsync(int lessonId, string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null && await userManager.IsInRoleAsync(user, DefaultRoles.SuperAdmin))
            return true;
        if (user is not null && await userManager.IsInRoleAsync(user, DefaultRoles.Admin))
            return true;
            
        var lesson = await context.Lessons
            .AsNoTracking()
            .Include(l => l.Section).ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        return lesson?.Section.Course.CreatedById == userId;
    }
}

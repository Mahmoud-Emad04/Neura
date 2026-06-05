using CloudinaryDotNet;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Entities;
using Neura.Core.Errors;
using Neura.Core.Settings;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.Video.GetSignedVideoUpload;

internal sealed class GetSignedVideoUploadHandler(
    Cloudinary cloudinary,
    CloudinarySettings cloudinarySettings,
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<GetSignedVideoUploadHandler> logger) 
    : IRequestHandler<GetSignedVideoUploadCommand, Result<SignedVideoUploadResponse>>
{
    public async Task<Result<SignedVideoUploadResponse>> Handle(
        GetSignedVideoUploadCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var userId = command.UserId;

        var lesson = await context.Lessons
            .AsNoTracking()
            .Include(l => l.Section).ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null)
            return Result.Failure<SignedVideoUploadResponse>(LessonErrors.NotFound);

        var isAuthorized = await IsUserAuthorizedAsync(lessonId, userId, ct);
        if (!isAuthorized)
            return Result.Failure<SignedVideoUploadResponse>(LessonErrors.UnauthorizedModification);

        var publicId = $"lesson_{lessonId}_{Guid.NewGuid():N}";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(cloudinarySettings.SignatureExpirationMinutes).ToUnixTimeSeconds();

        var uploadParams = new Dictionary<string, object>
        {
            { "folder", cloudinarySettings.FolderName },
            { "public_id", publicId },
            { "timestamp", timestamp }
        };

        var signature = cloudinary.Api.SignParameters(uploadParams);

        try
        {
            var response = new SignedVideoUploadResponse(
                CloudName: cloudinarySettings.CloudName,
                UploadUrl: $"https://api.cloudinary.com/v1_1/{cloudinarySettings.CloudName}/video/upload",
                ApiKey: cloudinarySettings.ApiKey,
                Signature: signature,
                Timestamp: timestamp,
                Folder: cloudinarySettings.FolderName,
                PublicId: publicId,
                MaxFileSize: cloudinarySettings.MaxVideoSizeMB * 1024 * 1024L,
                AllowedFormats: cloudinarySettings.AllowedFormats
            );

            logger.LogInformation("Generated signed upload credentials for lesson {LessonId} by user {UserId}", lessonId, userId);
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating signed upload credentials for lesson {LessonId}", lessonId);
            return Result.Failure<SignedVideoUploadResponse>(
                new Neura.Core.Abstractions.Error("Video.SignatureError", "Failed to generate upload credentials.", StatusCodes.Status500InternalServerError));
        }
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

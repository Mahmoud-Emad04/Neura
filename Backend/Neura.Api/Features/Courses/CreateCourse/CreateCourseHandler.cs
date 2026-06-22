using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Neura.Api.Infrastructure;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.FilesConsts;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.CreateCourse;

internal sealed class CreateCourseHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IFileService fileService,
    IServiceHelpers helpers,
    ILogger<CreateCourseHandler> logger,
    HybridCache hybridCache)
    : IRequestHandler<CreateCourseCommand, Result<CourseMetadataResponse>>
{
    public async Task<Result<CourseMetadataResponse>> Handle(
        CreateCourseCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var userId = command.UserId;

        var tags = await context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(ct);

        if (tags.Count != request.Tags.Count)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseTagNotFound);

        var ownerUser = await userManager.FindByIdAsync(userId);

        if (ownerUser is null)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.UserNotFound);

        var ownerRole = await context.CourseRoles
            .FirstOrDefaultAsync(r => r.Level == (int)CourseRoleType.CourseOwner, ct);

        if (ownerRole is null)
        {
            logger.LogError("CourseOwner role not found in database. Please run database seeding.");
            return Result.Failure<CourseMetadataResponse>(
                new Error("Course.RoleNotFound", "Course role configuration is missing", StatusCodes.Status500InternalServerError));
        }

        var course = request.Adapt<Course>();

        course.DisplayInstructorName = request.InstructorName;
        course.CreatedById = userId;
        course.CreatedOn = DateTime.UtcNow;
        course.Tags = tags;

        if (request.Image is not null)
            course.ImageUrl = await fileService.UploadImageAsync(request.Image, ImageConsts.Course, ct);
        else
            course.ImageUrl = Path.Combine("Images", ImageConsts.Course, ImageConsts.DefaultCourseImage);

        logger.LogInformation("Image with Title {title} & Url {imageurl} Created", course.Title, course.ImageUrl);

        context.Courses.Add(course);

        var courseUser = new CourseUser
        {
            Course = course,
            UserId = userId,
            CourseRoleId = ownerRole.Id,
            PermissionMask = ownerRole.PermissionMask,
            EnrolledOn = DateTime.UtcNow,
            EnrolledById = null
        };

        await context.CourseUsers.AddAsync(courseUser, ct);
        await context.SaveChangesAsync(ct);

        // Invalidate course caches
        await hybridCache.RemoveAsync(CacheKeys.CourseFullContent, ct);

        logger.LogInformation(
            "Course {CourseId} '{Title}' created by user {UserId}. Status: {Status}",
            course.Id,
            course.Title,
            userId,
            course.Status);

        var response = course.Adapt<CourseMetadataResponse>() with
        {
            KeyId = helpers.Encode(course.Id),
            InstructorName = course.DisplayInstructorName ?? $"{ownerUser.FirstName} {ownerUser.LastName}",
            ImageUrl = Path.Combine(helpers.GetBaseUrl(), course.ImageUrl),
            Tags = course.Tags.Select(c => new CourseMetadataTagResponse(c.Name, c.Id)).ToList(),
            NumberOfStudents = 0,
            IsEnrolled = true,
            IsBookmarked = false,
            IsOwner = true
        };

        return Result.Success(response);
    }
}

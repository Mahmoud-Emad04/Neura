using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Entities;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.Video.GetVideoLink;

internal sealed class GetVideoLinkHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) 
    : IRequestHandler<GetVideoLinkQuery, Result<VideoLinkResponse>>
{
    public async Task<Result<VideoLinkResponse>> Handle(
        GetVideoLinkQuery query, CancellationToken ct)
    {
        var lessonId = query.LessonId;
        var userId = query.UserId;

        var lesson = await context.Lessons
            .AsNoTracking()
            .Include(l => l.Section)
            .ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null)
            return Result.Failure<VideoLinkResponse>(LessonErrors.NotFound);

        if (string.IsNullOrWhiteSpace(lesson.CloudinaryVideoUrl))
            return Result.Failure<VideoLinkResponse>(LessonErrors.VideoNotFound);

        if (!lesson.IsPublished)
            return Result.Failure<VideoLinkResponse>(LessonErrors.NotFound);

        var isInstructor = await IsUserAuthorizedAsync(lessonId, userId, ct);

        if (lesson.IsVideoPrivate && isInstructor)
            return Result.Failure<VideoLinkResponse>(LessonErrors.UnauthorizedModification);

        if (!isInstructor && !lesson.IsPreview)
        {
            var isEnrolled = await context.CourseUsers
                .AnyAsync(cu => cu.CourseId == lesson.Section.CourseId && cu.UserId == userId, ct);

            if (!isEnrolled)
                return Result.Failure<VideoLinkResponse>(LessonErrors.NotEnrolled);
        }

        return Result.Success(new VideoLinkResponse(
            LessonId: lesson.Id,
            VideoUrl: lesson.CloudinaryVideoUrl,
            DurationSeconds: lesson.Duration.TotalSeconds,
            IsVideoPrivate: lesson.IsVideoPrivate
        ));
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

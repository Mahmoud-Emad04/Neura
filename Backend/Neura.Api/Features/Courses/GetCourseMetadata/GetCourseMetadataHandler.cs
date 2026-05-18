using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.GetCourseMetadata;

internal sealed class GetCourseMetadataHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IServiceHelpers helpers) 
    : IRequestHandler<GetCourseMetadataQuery, Result<CourseMetadataResponse>>
{
    public async Task<Result<CourseMetadataResponse>> Handle(
        GetCourseMetadataQuery request, CancellationToken ct)
    {
        if (!TryDecodeCourseId(request.CourseIdKey, out var courseId))
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        var course = await context.Courses
            .AsNoTracking()
            .Include(c => c.Tags)
            .Include(c => c.Prerequisites)
            .Include(c => c.LearningOutcomes)
            .SingleOrDefaultAsync(c => c.Id == courseId, ct);

        if (course is null)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        var ownerUser = await userManager.FindByIdAsync(course.CreatedById);

        if (ownerUser is null)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        CourseUser? userCourseRole = null;
        if (request.UserId is not null)
        {
            userCourseRole = await context.CourseUsers
                .AsNoTracking()
                .Include(cu => cu.CourseRole)
                .FirstOrDefaultAsync(cu =>
                        cu.CourseId == courseId &&
                        cu.UserId == request.UserId &&
                        !cu.IsDeleted,
                    ct);
        }

        var studentLevel = (int)CourseRoleType.Student;

        var numberOfStudents = await context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .CountAsync(cu =>
                    cu.CourseId == courseId &&
                    cu.CourseRole.Level == studentLevel &&
                    !cu.IsDeleted,
                ct);

        var isBookmarked = request.UserId is not null &&
                           await context.CourseBookmarks.AnyAsync(
                               cb => cb.CourseId == courseId && cb.UserId == request.UserId && !cb.IsDeleted,
                               ct);

        var response = course.Adapt<CourseMetadataResponse>() with
        {
            InstructorName = course.DisplayInstructorName ?? $"{ownerUser.FirstName} {ownerUser.LastName}",
            ImageUrl = Path.Combine(helpers.GetBaseUrl(), course.ImageUrl),
            Tags = course.Tags.Select(c => new CourseMetadataTagResponse(c.Name, c.Id)).ToList(),
            NumberOfStudents = numberOfStudents,
            IsEnrolled = userCourseRole is not null,
            IsBookmarked = isBookmarked,
            IsOwner = userCourseRole?.CourseRole.Level == (int)CourseRoleType.CourseOwner
        };

        return Result.Success(response);
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }
        courseId = numbers[0];
        return true;
    }
}

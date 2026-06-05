using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Enrollment.Enroll;

internal sealed class EnrollHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<EnrollHandler> logger,
    IServiceHelpers helpers) 
    : IRequestHandler<EnrollCommand, Result<EnrollmentResponse>>
{
    public async Task<Result<EnrollmentResponse>> Handle(
        EnrollCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.UserNotFound);

        if (!user.EmailConfirmed) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.EmailNotVerified);

        if (!TryDecodeCourseId(request.CourseId, out var courseId))
            return Result.Failure<EnrollmentResponse>(CourseErrors.CourseNotFound);

        var course = await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted, ct);

        if (course is null) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.CourseNotFound);

        if (course.Status == CourseStatus.Pending) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.CourseNotActive);

        var existingEnrollment = await context.CourseUsers
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == request.UserId, ct);

        if (existingEnrollment is not null)
        {
            if (!existingEnrollment.IsDeleted)
                return Result.Failure<EnrollmentResponse>(EnrollmentErrors.AlreadyEnrolled);

            existingEnrollment.IsDeleted = false;
            existingEnrollment.EnrolledOn = DateTime.UtcNow;
            existingEnrollment.LastAccessedOn = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);
            logger.LogInformation("User {UserId} re-enrolled in course {CourseId}", request.UserId, courseId);

            return Result.Success(await MapToEnrollmentResponseAsync(existingEnrollment, course, user));
        }

        if (course.Price > 0)
            return Result.Failure<EnrollmentResponse>(EnrollmentErrors.CourseRequiresPayment);

        var studentRole = await context.CourseRoles
            .FirstAsync(r => r.Level == (int)CourseRoleType.Student, ct);

        var courseUser = new CourseUser
        {
            CourseId = courseId,
            UserId = request.UserId,
            CourseRoleId = studentRole.Id,
            PermissionMask = studentRole.PermissionMask,
            EnrolledOn = DateTime.UtcNow,
            LastAccessedOn = DateTime.UtcNow
        };

        context.CourseUsers.Add(courseUser);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} enrolled in course {CourseId}", request.UserId, courseId);

        return Result.Success(await MapToEnrollmentResponseAsync(courseUser, course, user));
    }

    private async Task<EnrollmentResponse> MapToEnrollmentResponseAsync(CourseUser courseUser, Course course, ApplicationUser user)
    {
        var role = courseUser.CourseRole ?? await context.CourseRoles.FirstAsync(r => r.Id == courseUser.CourseRoleId);
        return new EnrollmentResponse
        {
            CourseId = course.Id,
            CourseName = course.Title,
            CourseThumbnail = course.ImageUrl,
            UserId = user.Id,
            UserName = $"{user.FirstName} {user.LastName}",
            Role = (CourseRoleType)role.Level,
            RoleName = role.Name,
            EnrolledOn = courseUser.EnrolledOn,
            LastAccessedOn = courseUser.LastAccessedOn,
            IsActive = !courseUser.IsDeleted
        };
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

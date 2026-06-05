using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Errors;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Enrollment.AddStudent;

internal sealed class AddStudentHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<AddStudentHandler> logger) 
    : IRequestHandler<AddStudentCommand, Result<EnrollmentResponse>>
{
    public async Task<Result<EnrollmentResponse>> Handle(
        AddStudentCommand request, CancellationToken ct)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId && !c.IsDeleted, ct);
        if (course is null) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.CourseNotFound);

        var requester = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == request.CourseId && cu.UserId == request.RequesterId && !cu.IsDeleted, ct);

        if (requester is null || !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageStudents))
            return Result.Failure<EnrollmentResponse>(EnrollmentErrors.CannotRemoveStudent);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null) return Result.Failure<EnrollmentResponse>(EnrollmentErrors.UserNotFound);

        var existingEnrollment = await context.CourseUsers
            .FirstOrDefaultAsync(cu => cu.CourseId == request.CourseId && cu.UserId == user.Id, ct);

        if (existingEnrollment is not null && !existingEnrollment.IsDeleted)
            return Result.Failure<EnrollmentResponse>(EnrollmentErrors.AlreadyEnrolled);

        var studentRole = await context.CourseRoles.FirstAsync(r => r.Level == (int)CourseRoleType.Student, ct);

        if (existingEnrollment is not null)
        {
            existingEnrollment.IsDeleted = false;
            existingEnrollment.EnrolledOn = DateTime.UtcNow;
            existingEnrollment.EnrolledById = request.RequesterId;
            existingEnrollment.CourseRoleId = studentRole.Id;
            existingEnrollment.PermissionMask = studentRole.PermissionMask;

            await context.SaveChangesAsync(ct);
            logger.LogInformation("User {RequesterId} re-added student {UserId} to course {CourseId}", request.RequesterId, user.Id, request.CourseId);

            return Result.Success(await MapToEnrollmentResponseAsync(existingEnrollment, course, user));
        }

        var courseUser = new CourseUser
        {
            CourseId = request.CourseId,
            UserId = user.Id,
            CourseRoleId = studentRole.Id,
            PermissionMask = studentRole.PermissionMask,
            EnrolledOn = DateTime.UtcNow,
            EnrolledById = request.RequesterId
        };

        context.CourseUsers.Add(courseUser);
        await context.SaveChangesAsync(ct);
        
        logger.LogInformation("User {RequesterId} added student {UserId} to course {CourseId}", request.RequesterId, user.Id, request.CourseId);

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
}

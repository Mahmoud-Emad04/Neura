using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Enrollment.RemoveStudent;

internal sealed class RemoveStudentHandler(
    ApplicationDbContext context,
    ILogger<RemoveStudentHandler> logger)
    : IRequestHandler<RemoveStudentCommand, Result>
{
    public async Task<Result> Handle(
        RemoveStudentCommand request, CancellationToken ct)
    {
        var requester = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == request.CourseId && cu.UserId == request.RequesterId && !cu.IsDeleted, ct);

        if (requester is null || !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageStudents))
            return Result.Failure(EnrollmentErrors.CannotRemoveStudent);

        var student = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == request.CourseId && cu.UserId == request.StudentId && !cu.IsDeleted, ct);

        if (student is null) return Result.Failure(EnrollmentErrors.StudentNotFound);

        if (student.CourseRole.Level > (int)CourseRoleType.Student)
            return Result.Failure(EnrollmentErrors.CannotUnenrollTeamMember);

        student.IsDeleted = true;
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {RequesterId} removed student {StudentId} from course {CourseId}",
            request.RequesterId, request.StudentId, request.CourseId);

        return Result.Success();
    }
}

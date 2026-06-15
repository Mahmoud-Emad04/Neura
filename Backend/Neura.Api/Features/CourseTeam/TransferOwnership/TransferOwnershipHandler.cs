using MediatR;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.TransferOwnership;

internal sealed class TransferOwnershipHandler(
    ApplicationDbContext context,
    ILogger<TransferOwnershipHandler> logger)
    : IRequestHandler<TransferOwnershipCommand, Result>
{
    public async Task<Result> Handle(
        TransferOwnershipCommand command, CancellationToken ct)
    {
        if (command.Request.NewOwnerId == command.RequesterId)
            return Result.Failure(CourseTeamErrors.TransferToSelf);

        var currentOwner = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == command.CourseId &&
                cu.UserId == command.RequesterId &&
                !cu.IsDeleted &&
                cu.CourseRole.Level == (int)CourseRoleType.CourseOwner, ct);

        if (currentOwner is null) return Result.Failure(CourseTeamErrors.InsufficientPermission);

        var newOwner = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == command.CourseId &&
                cu.UserId == command.Request.NewOwnerId &&
                !cu.IsDeleted, ct);

        if (newOwner is null) return Result.Failure(CourseTeamErrors.TransferTargetNotTeamMember);

        var ownerRole = await context.CourseRoles
            .FirstAsync(r => r.Level == (int)CourseRoleType.CourseOwner, ct);

        var coInstructorRole = await context.CourseRoles
            .FirstAsync(r => r.Level == (int)CourseRoleType.CoInstructor, ct);

        currentOwner.CourseRoleId = coInstructorRole.Id;
        currentOwner.PermissionMask = coInstructorRole.PermissionMask;

        newOwner.CourseRoleId = ownerRole.Id;
        newOwner.PermissionMask = ownerRole.PermissionMask;

        var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == command.CourseId, ct);
        if (course is not null) course.CreatedById = command.Request.NewOwnerId;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Ownership of course {CourseId} transferred from {OldOwnerId} to {NewOwnerId}",
            command.CourseId, command.RequesterId, command.Request.NewOwnerId);

        return Result.Success();
    }
}

using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.ChangeTeamRole;

internal sealed class ChangeTeamRoleHandler(
    ApplicationDbContext context,
    ILogger<ChangeTeamRoleHandler> logger)
    : IRequestHandler<ChangeTeamRoleCommand, Result<TeamMemberResponse>>
{
    public async Task<Result<TeamMemberResponse>> Handle(
        ChangeTeamRoleCommand command, CancellationToken ct)
    {
        var newRole = await context.CourseRoles
            .FirstOrDefaultAsync(r => r.Level == (int)command.Request.NewRole, ct);

        if (newRole is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvalidRole);

        if (command.Request.NewRole == CourseRoleType.CourseOwner)
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.CannotAssignOwnerRole);

        var requester = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == command.CourseId && cu.UserId == command.RequesterId && !cu.IsDeleted, ct);

        if (requester is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InsufficientPermission);

        if (!CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InsufficientPermission);

        var targetMember = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .Include(cu => cu.User)
            .FirstOrDefaultAsync(cu => cu.CourseId == command.CourseId && cu.UserId == command.UserId && !cu.IsDeleted, ct);

        if (targetMember is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.MemberNotFound);

        if (targetMember.CourseRole.Level == (int)CourseRoleType.CourseOwner)
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.CannotChangeOwnerRole);

        if (requester.CourseRole.Level != (int)CourseRoleType.CourseOwner &&
            targetMember.CourseRole.Level >= requester.CourseRole.Level)
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.CannotManageHigherRole);

        targetMember.CourseRoleId = newRole.Id;
        targetMember.PermissionMask = newRole.PermissionMask;

        await context.SaveChangesAsync(ct);
        await context.Entry(targetMember).Reference(cu => cu.CourseRole).LoadAsync(ct);

        logger.LogInformation("User {RequesterId} changed {UserId}'s role to {NewRole} in course {CourseId}",
            command.RequesterId, command.UserId, command.Request.NewRole, command.CourseId);

        return Result.Success(CourseTeamHelpers.MapToTeamMemberResponse(targetMember));
    }
}

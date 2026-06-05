using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.Authorization;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.RemoveTeamMember;

internal sealed class RemoveTeamMemberHandler(
    ApplicationDbContext context,
    ILogger<RemoveTeamMemberHandler> logger) 
    : IRequestHandler<RemoveTeamMemberCommand, Result>
{
    public async Task<Result> Handle(
        RemoveTeamMemberCommand command, CancellationToken ct)
    {
        if (command.UserId == command.RequesterId) 
            return Result.Failure(CourseTeamErrors.CannotRemoveSelf);

        var requester = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == command.CourseId && cu.UserId == command.RequesterId && !cu.IsDeleted, ct);

        if (requester is null) return Result.Failure(CourseTeamErrors.InsufficientPermission);

        if (!CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure(CourseTeamErrors.InsufficientPermission);

        var targetMember = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == command.CourseId && cu.UserId == command.UserId && !cu.IsDeleted, ct);

        if (targetMember is null) return Result.Failure(CourseTeamErrors.MemberNotFound);

        if (targetMember.CourseRole.Level == (int)CourseRoleType.CourseOwner)
            return Result.Failure(CourseTeamErrors.CannotRemoveOwner);

        if (requester.CourseRole.Level != (int)CourseRoleType.CourseOwner &&
            targetMember.CourseRole.Level >= requester.CourseRole.Level)
            return Result.Failure(CourseTeamErrors.CannotManageHigherRole);

        targetMember.IsDeleted = true;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {RequesterId} removed {UserId} from course {CourseId}",
            command.RequesterId, command.UserId, command.CourseId);

        return Result.Success();
    }
}

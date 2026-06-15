using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.CancelInvitation;

internal sealed class CancelInvitationHandler(
    ApplicationDbContext context,
    ILogger<CancelInvitationHandler> logger)
    : IRequestHandler<CancelInvitationCommand, Result>
{
    public async Task<Result> Handle(
        CancelInvitationCommand command, CancellationToken ct)
    {
        var requester = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == command.CourseId && cu.UserId == command.RequesterId && !cu.IsDeleted, ct);

        if (requester is null ||
            !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure(CourseTeamErrors.InsufficientPermission);

        var invitation = await context.CourseInvitations
            .FirstOrDefaultAsync(ci =>
                ci.Id == command.InvitationId &&
                ci.CourseId == command.CourseId &&
                ci.Status == InvitationStatus.Pending, ct);

        if (invitation is null) return Result.Failure(CourseTeamErrors.InvitationNotFound);

        invitation.Status = InvitationStatus.Cancelled;
        invitation.RespondedOn = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Invitation {InvitationId} cancelled by {RequesterId}", command.InvitationId, command.RequesterId);

        return Result.Success();
    }
}

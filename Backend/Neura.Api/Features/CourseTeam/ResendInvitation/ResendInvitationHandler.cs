using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.ResendInvitation;

internal sealed class ResendInvitationHandler(
    ApplicationDbContext context,
    ILogger<ResendInvitationHandler> logger)
    : IRequestHandler<ResendInvitationCommand, Result<CourseInvitationResponse>>
{
    public async Task<Result<CourseInvitationResponse>> Handle(
        ResendInvitationCommand command, CancellationToken ct)
    {
        var requester = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == command.CourseId && cu.UserId == command.RequesterId && !cu.IsDeleted, ct);

        if (requester is null ||
            !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.InsufficientPermission);

        var invitation = await context.CourseInvitations
            .Include(ci => ci.Course)
            .Include(ci => ci.CourseRole)
            .Include(ci => ci.InvitedBy)
            .FirstOrDefaultAsync(ci =>
                ci.Id == command.InvitationId &&
                ci.CourseId == command.CourseId &&
                ci.Status == InvitationStatus.Pending, ct);

        if (invitation is null) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.InvitationNotFound);

        invitation.ExpiresOn = DateTime.UtcNow.AddDays(CourseLimits.InvitationExpiryDays);

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Invitation {InvitationId} resent by {RequesterId}", command.InvitationId, command.RequesterId);

        return Result.Success(CourseTeamHelpers.MapToInvitationResponse(invitation));
    }
}

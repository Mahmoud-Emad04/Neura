using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Api.Features.CourseTeam;

namespace Neura.Api.Features.Invitations.AcceptInvitation;

internal sealed class AcceptInvitationHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<AcceptInvitationHandler> logger) 
    : IRequestHandler<AcceptInvitationCommand, Result<TeamMemberResponse>>
{
    public async Task<Result<TeamMemberResponse>> Handle(
        AcceptInvitationCommand command, CancellationToken ct)
    {
        var invitation = await context.CourseInvitations
            .Include(ci => ci.Course)
            .Include(ci => ci.CourseRole)
            .FirstOrDefaultAsync(ci => ci.Token == command.Token, ct);

        if (invitation is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvalidToken);

        if (invitation.Status != InvitationStatus.Pending)
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvitationAlreadyResponded);

        if (DateTime.UtcNow >= invitation.ExpiresOn)
        {
            invitation.Status = InvitationStatus.Expired;
            await context.SaveChangesAsync(ct);
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvitationExpired);
        }

        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.UserNotFound);

        if (user.Email?.ToLowerInvariant() != invitation.Email.ToLowerInvariant())
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvalidToken);

        var existingMembership = await context.CourseUsers
            .AnyAsync(cu => cu.CourseId == invitation.CourseId && cu.UserId == command.UserId && !cu.IsDeleted, ct);

        if (existingMembership) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.AlreadyTeamMember);

        var courseUser = new CourseUser
        {
            CourseId = invitation.CourseId,
            UserId = command.UserId,
            CourseRoleId = invitation.CourseRoleId,
            PermissionMask = invitation.CourseRole.PermissionMask,
            EnrolledOn = DateTime.UtcNow,
            EnrolledById = invitation.InvitedById,
            InvitationId = invitation.Id
        };

        context.CourseUsers.Add(courseUser);

        invitation.Status = InvitationStatus.Accepted;
        invitation.RespondedOn = DateTime.UtcNow;
        invitation.AcceptedUserId = command.UserId;

        await context.SaveChangesAsync(ct);

        await context.Entry(courseUser).Reference(cu => cu.User).LoadAsync(ct);
        await context.Entry(courseUser).Reference(cu => cu.CourseRole).LoadAsync(ct);
        await context.Entry(courseUser).Reference(cu => cu.EnrolledBy).LoadAsync(ct);

        logger.LogInformation("User {UserId} accepted invitation {InvitationId} for course {CourseId}",
            command.UserId, invitation.Id, invitation.CourseId);

        return Result.Success(CourseTeamHelpers.MapToTeamMemberResponse(courseUser));
    }
}

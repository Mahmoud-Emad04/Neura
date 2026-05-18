using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.Authorization;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.InviteTeamMember;

internal sealed class InviteTeamMemberHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<InviteTeamMemberHandler> logger) 
    : IRequestHandler<InviteTeamMemberCommand, Result<CourseInvitationResponse>>
{
    public async Task<Result<CourseInvitationResponse>> Handle(
        InviteTeamMemberCommand command, CancellationToken ct)
    {
        var normalizedEmail = command.Request.Email.Trim().ToLowerInvariant();

        var course = await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CourseId && !c.IsDeleted, ct);

        if (course is null) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.CourseNotFound);

        var requester = await context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == command.CourseId && cu.UserId == command.InviterId && !cu.IsDeleted, ct);

        if (requester is null ||
            !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.InsufficientPermission);

        var inviterUser = await userManager.FindByIdAsync(command.InviterId);
        if (inviterUser?.Email?.ToLowerInvariant() == normalizedEmail)
            return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.CannotInviteSelf);

        if (command.Request.Role == CourseRoleType.CourseOwner)
            return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.CannotAssignOwnerRole);

        if (command.Request.Role >= CourseRoleType.Assistant)
        {
            var teamMemberCount = await context.CourseUsers
                .Include(cu => cu.CourseRole)
                .CountAsync(cu =>
                    cu.CourseId == command.CourseId &&
                    !cu.IsDeleted &&
                    cu.CourseRole.Level >= (int)CourseRoleType.Assistant, ct);

            if (teamMemberCount >= CourseLimits.MaxTeamMembers)
                return Result.Failure<CourseInvitationResponse>(
                    CourseTeamErrors.TeamLimitReached(CourseLimits.MaxTeamMembers));
        }

        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            var existingMembership = await context.CourseUsers
                .AnyAsync(cu =>
                    cu.CourseId == command.CourseId &&
                    cu.UserId == existingUser.Id &&
                    !cu.IsDeleted, ct);

            if (existingMembership) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.AlreadyTeamMember);
        }

        var existingInvitation = await context.CourseInvitations
            .AnyAsync(ci =>
                ci.CourseId == command.CourseId &&
                ci.Email.ToLower() == normalizedEmail &&
                ci.Status == InvitationStatus.Pending, ct);

        if (existingInvitation) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.AlreadyInvited);

        var role = await context.CourseRoles
            .FirstOrDefaultAsync(r => r.Level == (int)command.Request.Role, ct);

        if (role is null) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.InvalidRole);

        var invitation = new CourseInvitation
        {
            CourseId = command.CourseId,
            Email = normalizedEmail,
            Token = CourseTeamHelpers.GenerateInvitationToken(),
            CourseRoleId = role.Id,
            Status = InvitationStatus.Pending,
            CustomMessage = command.Request.CustomMessage?.Trim(),
            InvitedById = command.InviterId,
            InvitedOn = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.AddDays(CourseLimits.InvitationExpiryDays)
        };

        context.CourseInvitations.Add(invitation);
        await context.SaveChangesAsync(ct);

        await context.Entry(invitation).Reference(i => i.Course).LoadAsync(ct);
        await context.Entry(invitation).Reference(i => i.CourseRole).LoadAsync(ct);
        await context.Entry(invitation).Reference(i => i.InvitedBy).LoadAsync(ct);

        logger.LogInformation("User {InviterId} invited {Email} to course {CourseId} as {Role}",
            command.InviterId, normalizedEmail, command.CourseId, command.Request.Role);

        return Result.Success(CourseTeamHelpers.MapToInvitationResponse(invitation));
    }
}

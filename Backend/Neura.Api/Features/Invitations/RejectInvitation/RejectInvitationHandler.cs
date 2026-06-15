using MediatR;
using Microsoft.AspNetCore.Identity;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Invitations.RejectInvitation;

internal sealed class RejectInvitationHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<RejectInvitationHandler> logger)
    : IRequestHandler<RejectInvitationCommand, Result>
{
    public async Task<Result> Handle(
        RejectInvitationCommand command, CancellationToken ct)
    {
        var invitation = await context.CourseInvitations
            .FirstOrDefaultAsync(ci => ci.Token == command.Token, ct);

        if (invitation is null) return Result.Failure(CourseTeamErrors.InvalidToken);

        if (invitation.Status != InvitationStatus.Pending)
            return Result.Failure(CourseTeamErrors.InvitationAlreadyResponded);

        if (!string.IsNullOrEmpty(command.UserId))
        {
            var user = await userManager.FindByIdAsync(command.UserId);
            if (user?.Email?.ToLowerInvariant() != invitation.Email.ToLowerInvariant())
                return Result.Failure(CourseTeamErrors.InvalidToken);
        }

        invitation.Status = InvitationStatus.Rejected;
        invitation.RespondedOn = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Invitation {InvitationId} rejected", invitation.Id);

        return Result.Success();
    }
}

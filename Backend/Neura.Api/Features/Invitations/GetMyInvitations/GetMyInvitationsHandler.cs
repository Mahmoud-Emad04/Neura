using MediatR;
using Neura.Api.Features.CourseTeam;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Invitations.GetMyInvitations;

internal sealed class GetMyInvitationsHandler(ApplicationDbContext context)
    : IRequestHandler<GetMyInvitationsQuery, Result<MyInvitationsResponse>>
{
    public async Task<Result<MyInvitationsResponse>> Handle(
        GetMyInvitationsQuery query, CancellationToken ct)
    {
        var normalizedEmail = query.UserEmail.ToLowerInvariant();

        var pendingInvitations = await context.CourseInvitations
            .AsNoTracking()
            .Include(ci => ci.Course)
            .Include(ci => ci.CourseRole)
            .Include(ci => ci.InvitedBy)
            .Where(ci =>
                ci.Email.ToLower() == normalizedEmail &&
                ci.Status == InvitationStatus.Pending &&
                ci.ExpiresOn > DateTime.UtcNow)
            .OrderByDescending(ci => ci.InvitedOn)
            .Select(ci => CourseTeamHelpers.MapToInvitationResponse(ci))
            .ToListAsync(ct);

        return Result.Success(new MyInvitationsResponse
        {
            PendingInvitations = pendingInvitations,
            TotalPending = pendingInvitations.Count
        });
    }
}

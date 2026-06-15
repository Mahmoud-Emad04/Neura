using MediatR;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.GetPendingInvitations;

internal sealed class GetPendingInvitationsHandler(ApplicationDbContext context)
    : IRequestHandler<GetPendingInvitationsQuery, Result<List<CourseInvitationResponse>>>
{
    public async Task<Result<List<CourseInvitationResponse>>> Handle(
        GetPendingInvitationsQuery query, CancellationToken ct)
    {
        var courseExists = await context.Courses
            .AnyAsync(c => c.Id == query.CourseId && !c.IsDeleted, ct);

        if (!courseExists) return Result.Failure<List<CourseInvitationResponse>>(CourseTeamErrors.CourseNotFound);

        var invitations = await context.CourseInvitations
            .AsNoTracking()
            .Include(ci => ci.CourseRole)
            .Include(ci => ci.InvitedBy)
            .Include(ci => ci.Course)
            .Where(ci => ci.CourseId == query.CourseId && ci.Status == InvitationStatus.Pending)
            .OrderByDescending(ci => ci.InvitedOn)
            .Select(ci => CourseTeamHelpers.MapToInvitationResponse(ci))
            .ToListAsync(ct);

        return Result.Success(invitations);
    }
}

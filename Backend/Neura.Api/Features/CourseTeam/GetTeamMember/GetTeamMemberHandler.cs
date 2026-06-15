using MediatR;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.GetTeamMember;

internal sealed class GetTeamMemberHandler(ApplicationDbContext context)
    : IRequestHandler<GetTeamMemberQuery, Result<TeamMemberResponse>>
{
    public async Task<Result<TeamMemberResponse>> Handle(
        GetTeamMemberQuery query, CancellationToken ct)
    {
        var member = await context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.CourseRole)
            .Include(cu => cu.EnrolledBy)
            .Where(cu => cu.CourseId == query.CourseId && cu.UserId == query.UserId && !cu.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (member is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.MemberNotFound);

        return Result.Success(CourseTeamHelpers.MapToTeamMemberResponse(member));
    }
}

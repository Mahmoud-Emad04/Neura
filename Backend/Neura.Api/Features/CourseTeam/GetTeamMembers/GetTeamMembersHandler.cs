using MediatR;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.GetTeamMembers;

internal sealed class GetTeamMembersHandler(ApplicationDbContext context)
    : IRequestHandler<GetTeamMembersQuery, Result<List<TeamMemberResponse>>>
{
    public async Task<Result<List<TeamMemberResponse>>> Handle(
        GetTeamMembersQuery query, CancellationToken ct)
    {
        var courseExists = await context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == query.CourseId && !c.IsDeleted, ct);

        if (!courseExists) return Result.Failure<List<TeamMemberResponse>>(CourseTeamErrors.CourseNotFound);

        var members = await context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.CourseRole)
            .Include(cu => cu.EnrolledBy)
            .Where(cu => cu.CourseId == query.CourseId && !cu.IsDeleted)
            .OrderByDescending(cu => cu.CourseRole.Level)
            .ThenBy(cu => cu.EnrolledOn)
            .Select(cu => CourseTeamHelpers.MapToTeamMemberResponse(cu))
            .ToListAsync(ct);

        return Result.Success(members);
    }
}

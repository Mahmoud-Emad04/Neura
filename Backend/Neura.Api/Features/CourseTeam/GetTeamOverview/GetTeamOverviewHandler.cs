using MediatR;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.CourseTeam.GetTeamOverview;

internal sealed class GetTeamOverviewHandler(ApplicationDbContext context)
    : IRequestHandler<GetTeamOverviewQuery, Result<TeamOverviewResponse>>
{
    public async Task<Result<TeamOverviewResponse>> Handle(
        GetTeamOverviewQuery query, CancellationToken ct)
    {
        var courseId = query.CourseId;

        var course = await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted, ct);

        if (course is null) return Result.Failure<TeamOverviewResponse>(CourseTeamErrors.CourseNotFound);

        var members = await context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.CourseRole)
            .Include(cu => cu.EnrolledBy)
            .Where(cu => cu.CourseId == courseId && !cu.IsDeleted)
            .OrderByDescending(cu => cu.CourseRole.Level)
            .ThenBy(cu => cu.EnrolledOn)
            .Select(cu => CourseTeamHelpers.MapToTeamMemberResponse(cu))
            .ToListAsync(ct);

        var pendingInvitations = await context.CourseInvitations
            .AsNoTracking()
            .Include(ci => ci.CourseRole)
            .Include(ci => ci.InvitedBy)
            .Include(ci => ci.Course)
            .Where(ci => ci.CourseId == courseId && ci.Status == InvitationStatus.Pending)
            .OrderByDescending(ci => ci.InvitedOn)
            .Select(ci => CourseTeamHelpers.MapToInvitationResponse(ci))
            .ToListAsync(ct);

        var teamMemberCount = members.Count(m => m.RoleLevel >= (int)CourseRoleType.Assistant);

        return Result.Success(new TeamOverviewResponse
        {
            CourseId = courseId,
            CourseName = course.Title,
            TotalMembers = teamMemberCount,
            MaxMembers = CourseLimits.MaxTeamMembers,
            CanInviteMore = teamMemberCount < CourseLimits.MaxTeamMembers,
            Members = members,
            PendingInvitations = pendingInvitations
        });
    }
}

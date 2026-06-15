using MediatR;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.CourseTeam.GetTeamMembers;

public sealed record GetTeamMembersQuery(int CourseId)
    : IRequest<Result<List<TeamMemberResponse>>>;

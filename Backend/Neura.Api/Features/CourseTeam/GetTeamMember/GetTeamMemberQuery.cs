using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.CourseTeam.GetTeamMember;

public sealed record GetTeamMemberQuery(int CourseId, string UserId) 
    : IRequest<Result<TeamMemberResponse>>;

using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.CourseTeam.ChangeTeamRole;

public sealed record ChangeTeamRoleCommand(int CourseId, string UserId, ChangeTeamRoleRequest Request, string RequesterId) 
    : IRequest<Result<TeamMemberResponse>>;

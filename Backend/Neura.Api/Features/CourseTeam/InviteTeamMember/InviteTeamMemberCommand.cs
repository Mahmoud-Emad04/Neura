using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.CourseTeam.InviteTeamMember;

public sealed record InviteTeamMemberCommand(int CourseId, InviteTeamMemberRequest Request, string InviterId) 
    : IRequest<Result<CourseInvitationResponse>>;

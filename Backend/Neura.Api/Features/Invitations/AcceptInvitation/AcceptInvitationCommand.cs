using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.Invitations.AcceptInvitation;

public sealed record AcceptInvitationCommand(string Token, string UserId) 
    : IRequest<Result<TeamMemberResponse>>;

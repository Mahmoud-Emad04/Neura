using MediatR;

namespace Neura.Api.Features.CourseTeam.RemoveTeamMember;

public sealed record RemoveTeamMemberCommand(int CourseId, string UserId, string RequesterId)
    : IRequest<Result>;

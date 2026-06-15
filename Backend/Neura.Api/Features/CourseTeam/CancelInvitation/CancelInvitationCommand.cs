using MediatR;

namespace Neura.Api.Features.CourseTeam.CancelInvitation;

public sealed record CancelInvitationCommand(int CourseId, int InvitationId, string RequesterId)
    : IRequest<Result>;

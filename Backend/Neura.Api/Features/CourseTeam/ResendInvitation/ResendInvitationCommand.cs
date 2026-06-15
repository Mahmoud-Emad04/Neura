using MediatR;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.CourseTeam.ResendInvitation;

public sealed record ResendInvitationCommand(int CourseId, int InvitationId, string RequesterId)
    : IRequest<Result<CourseInvitationResponse>>;

using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.CourseTeam.CancelInvitation;

public sealed record CancelInvitationCommand(int CourseId, int InvitationId, string RequesterId) 
    : IRequest<Result>;

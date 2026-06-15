using MediatR;

namespace Neura.Api.Features.Invitations.RejectInvitation;

public sealed record RejectInvitationCommand(string Token, string? UserId)
    : IRequest<Result>;

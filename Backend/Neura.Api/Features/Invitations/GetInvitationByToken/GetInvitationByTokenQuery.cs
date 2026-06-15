using MediatR;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.Invitations.GetInvitationByToken;

public sealed record GetInvitationByTokenQuery(string Token)
    : IRequest<Result<InvitationDetailsResponse>>;

using MediatR;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.Invitations.GetMyInvitations;

public sealed record GetMyInvitationsQuery(string UserEmail)
    : IRequest<Result<MyInvitationsResponse>>;

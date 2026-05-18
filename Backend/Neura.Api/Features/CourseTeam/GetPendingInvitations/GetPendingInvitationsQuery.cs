using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.CourseTeam.GetPendingInvitations;

public sealed record GetPendingInvitationsQuery(int CourseId) 
    : IRequest<Result<List<CourseInvitationResponse>>>;

using MediatR;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.CourseTeam.GetTeamOverview;

public sealed record GetTeamOverviewQuery(int CourseId, string RequesterId)
    : IRequest<Result<TeamOverviewResponse>>;

using MediatR;
using Neura.Core.Contracts.Analytics;

namespace Neura.Api.Features.ExamAnalytics.GetScoreDistribution;

public sealed record GetScoreDistributionQuery(int ExamId, string UserId)
    : IRequest<Result<ScoreDistributionResponse>>;

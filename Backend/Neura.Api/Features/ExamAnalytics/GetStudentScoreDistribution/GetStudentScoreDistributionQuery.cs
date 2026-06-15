using MediatR;
using Neura.Core.Contracts.Analytics;

namespace Neura.Api.Features.ExamAnalytics.GetStudentScoreDistribution;

public sealed record GetStudentScoreDistributionQuery(int ExamId, string UserId)
    : IRequest<Result<ScoreDistributionResponse>>;

using MediatR;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAnalytics.GetExamViolations;

public sealed record GetExamViolationsQuery(int ExamId, string UserId)
    : IRequest<Result<ExamViolationsListResponse>>;

using MediatR;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAnalytics.GetExamViolationById;

public sealed record GetExamViolationByIdQuery(int ExamId, int ViolationId, string UserId)
    : IRequest<Result<ExamViolationResponse>>;

using MediatR;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAttempts.SubmitAttempt;

public sealed record SubmitAttemptCommand(int AttemptId, string UserId)
    : IRequest<Result<SubmitAttemptResponse>>;

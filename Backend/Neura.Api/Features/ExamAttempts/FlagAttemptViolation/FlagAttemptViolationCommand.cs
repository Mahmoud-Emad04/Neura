using MediatR;

namespace Neura.Api.Features.ExamAttempts.FlagAttemptViolation;

public sealed record FlagAttemptViolationCommand(int AttemptId, string Reason, string UserId)
    : IRequest<Result>;

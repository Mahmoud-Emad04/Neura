using MediatR;

namespace Neura.Api.Features.ExamAttempts.ResolveViolation;

public sealed record ResolveViolationCommand(
    int AttemptId, decimal NewScore, string Notes, string UserId)
    : IRequest<Result>;

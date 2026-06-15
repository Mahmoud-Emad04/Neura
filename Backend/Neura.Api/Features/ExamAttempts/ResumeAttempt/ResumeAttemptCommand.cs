using MediatR;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAttempts.ResumeAttempt;

public sealed record ResumeAttemptCommand(int AttemptId, string UserId)
    : IRequest<Result<StartAttemptResponse>>;

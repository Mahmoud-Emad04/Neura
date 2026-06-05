using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAttempts.RecordViolation;

public sealed record RecordViolationCommand(int AttemptId, ViolationRequest Request, string UserId) 
    : IRequest<Result<ViolationResponse>>;

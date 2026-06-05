using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAttempts.StartAttempt;

public sealed record StartAttemptCommand(int LessonId, string UserId) 
    : IRequest<Result<StartAttemptResponse>>;

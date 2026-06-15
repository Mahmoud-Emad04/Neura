using MediatR;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAttempts.SaveAnswer;

public sealed record SaveAnswerCommand(int AttemptId, int QuestionId, SaveAnswerRequest Request, string UserId)
    : IRequest<Result>;

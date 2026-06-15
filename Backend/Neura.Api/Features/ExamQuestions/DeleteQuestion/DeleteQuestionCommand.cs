using MediatR;

namespace Neura.Api.Features.ExamQuestions.DeleteQuestion;

public sealed record DeleteQuestionCommand(int QuestionId, string UserId)
    : IRequest<Result>;

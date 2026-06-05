using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.ExamQuestions.DeleteQuestion;

public sealed record DeleteQuestionCommand(int QuestionId, string UserId) 
    : IRequest<Result>;

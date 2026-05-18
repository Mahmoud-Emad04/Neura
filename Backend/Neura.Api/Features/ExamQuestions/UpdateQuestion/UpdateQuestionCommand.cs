using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Question;

namespace Neura.Api.Features.ExamQuestions.UpdateQuestion;

public sealed record UpdateQuestionCommand(int QuestionId, UpdateQuestionRequest Request, string UserId) 
    : IRequest<Result<QuestionResponse>>;

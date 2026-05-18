using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Question;

namespace Neura.Api.Features.ExamQuestions.AddQuestion;

public sealed record AddQuestionCommand(int LessonId, CreateQuestionRequest Request, string UserId) 
    : IRequest<Result<QuestionResponse>>;

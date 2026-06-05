using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Question;

namespace Neura.Api.Features.ExamQuestions.ReorderQuestions;

public sealed record ReorderQuestionsCommand(int LessonId, ReorderQuestionsRequest Request, string UserId) 
    : IRequest<Result>;

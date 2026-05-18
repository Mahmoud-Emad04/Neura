using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Exams.DeleteExam;

public sealed record DeleteExamCommand(int LessonId, string UserId) 
    : IRequest<Result>;

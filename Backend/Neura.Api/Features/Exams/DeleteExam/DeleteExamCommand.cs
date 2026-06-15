using MediatR;

namespace Neura.Api.Features.Exams.DeleteExam;

public sealed record DeleteExamCommand(int LessonId, string UserId)
    : IRequest<Result>;

using MediatR;

namespace Neura.Api.Features.Exams.PublishExam;

public sealed record PublishExamCommand(int LessonId, string UserId)
    : IRequest<Result>;

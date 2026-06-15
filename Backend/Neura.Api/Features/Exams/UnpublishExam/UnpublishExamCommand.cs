using MediatR;

namespace Neura.Api.Features.Exams.UnpublishExam;

public sealed record UnpublishExamCommand(int LessonId, string UserId)
    : IRequest<Result>;

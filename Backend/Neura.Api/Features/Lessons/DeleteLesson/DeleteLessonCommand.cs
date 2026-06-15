using MediatR;

namespace Neura.Api.Features.Lessons.DeleteLesson;

public sealed record DeleteLessonCommand(int LessonId, string UserId)
    : IRequest<Result>;

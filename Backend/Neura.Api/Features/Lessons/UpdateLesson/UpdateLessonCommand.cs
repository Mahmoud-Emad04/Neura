using MediatR;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.UpdateLesson;

public sealed record UpdateLessonCommand(int LessonId, UpdateLessonRequest Request, string UserId)
    : IRequest<Result>;

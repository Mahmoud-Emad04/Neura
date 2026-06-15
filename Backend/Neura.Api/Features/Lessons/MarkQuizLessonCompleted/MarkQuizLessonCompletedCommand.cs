using MediatR;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.MarkQuizLessonCompleted;

public sealed record MarkQuizLessonCompletedCommand(int LessonId, string UserId)
    : IRequest<Result<LessonCompletionResponse>>;

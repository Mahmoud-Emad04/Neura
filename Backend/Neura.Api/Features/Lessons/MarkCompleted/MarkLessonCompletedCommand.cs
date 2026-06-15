using MediatR;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.MarkCompleted;

public sealed record MarkLessonCompletedCommand(int LessonId, string UserId)
    : IRequest<Result<LessonCompletionResponse>>;

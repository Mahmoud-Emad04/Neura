using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Lessons.MarkQuizLessonCompleted;

public sealed record MarkQuizLessonCompletedCommand(int LessonId, string UserId) 
    : IRequest;

using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Lessons.DeleteLesson;

public sealed record DeleteLessonCommand(int LessonId, string UserId) 
    : IRequest<Result>;

using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.UpdateLessonPosition;

public sealed record UpdateLessonPositionCommand(int LessonId, UpdateLessonPositionRequest Request, string UserId) 
    : IRequest<Result>;

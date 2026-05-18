using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.CourseProgress.GetNextLesson;

public sealed record GetNextLessonQuery(string CourseKeyId, string UserId) 
    : IRequest<Result<NextLessonResponse?>>;

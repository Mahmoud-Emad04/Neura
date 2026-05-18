using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.GetSectionLessons;

public sealed record GetSectionLessonsQuery(int SectionId, string UserId) 
    : IRequest<Result<List<LessonWithPositionResponse>>>;

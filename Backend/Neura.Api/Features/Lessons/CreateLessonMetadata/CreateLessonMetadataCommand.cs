using MediatR;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.CreateLessonMetadata;

public sealed record CreateLessonMetadataCommand(int SectionId, CreateLessonRequest Request, string UserId)
    : IRequest<Result<int>>;

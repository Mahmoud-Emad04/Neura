using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Core.Services;

public interface ILessonService
{
    Task<Result<int>> CreateLessonMetadataAsync(CreateLessonRequest request, CancellationToken cancellationToken = default);
    Task<Result> CompleteLessonAsync(int id, CompleteLessonRequest request, CancellationToken cancellationToken = default);
    Task<Result<LessonResponse>> GetLessonByIdAsync(int lessonId, string userId, CancellationToken cancellationToken = default);
    Task<Result<(string Path, string ContentType)>> GetLessonVideoPathAsync(
        int lessonId, string userId, CancellationToken ct);
}

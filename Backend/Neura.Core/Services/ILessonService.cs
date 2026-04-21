using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Core.Services;

public interface ILessonService
{
    Task<Result<int>> CreateLessonMetadataAsync(int sectionId,CreateLessonRequest request, string userId,
        CancellationToken cancellationToken = default);

    Task<Result<LessonResponse>> GetLessonByIdAsync(int lessonId, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the position of a lesson within its section.
    /// </summary>
    Task<Result> UpdateLessonPositionAsync(int lessonId, int newPosition, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the privacy status of a lesson's video.
    /// </summary>
    Task<Result> UpdateLessonPrivacyAsync(int lessonId, UpdateLessonPrivacyRequest request, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates basic lesson information.
    /// </summary>
    Task<Result> UpdateLessonAsync(int lessonId, UpdateLessonRequest request, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all lessons in a section with their position information.
    /// </summary>
    Task<Result<List<LessonWithPositionResponse>>> GetSectionLessonsAsync(int sectionId, string userId,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateArticleContentAsync(int lessonId, UpdateArticleRequest request, string userId,
        CancellationToken ct = default);

    Task<Result<ArticleResponse>> GetArticleContentAsync(int lessonId, string userId, CancellationToken ct = default);
}
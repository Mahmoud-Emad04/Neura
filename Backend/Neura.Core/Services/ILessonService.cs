using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Core.Services;

public interface ILessonService
{
    Task<Result<int>> CreateLessonMetadataAsync(CreateLessonRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> CompleteLessonAsync(int id, CompleteLessonRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LessonResponse>> GetLessonByIdAsync(int lessonId, string userId,
        CancellationToken cancellationToken = default);

    Task<Result<CloudinaryVideoResponse>> GetCloudinaryVideoAsync(int lessonId, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the position of a lesson within its section.
    /// </summary>
    Task<Result> UpdateLessonPositionAsync(int lessonId, int newPosition, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the privacy status of a lesson's video.
    /// </summary>
    Task<Result> UpdateLessonPrivacyAsync(int lessonId, UpdateLessonPrivacyRequest request, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates basic lesson information.
    /// </summary>
    Task<Result> UpdateLessonAsync(int lessonId, UpdateLessonRequest request, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a lesson and adjusts positions of remaining lessons.
    /// </summary>
    Task<Result> DeleteLessonAsync(int lessonId, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all lessons in a section with their position information.
    /// </summary>
    Task<Result<List<LessonWithPositionResponse>>> GetSectionLessonsAsync(int sectionId, string userId,
        CancellationToken cancellationToken = default);
}
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Core.Services;

public interface ILessonProgressService
{
    Task<Result<LessonCompletionResponse>> MarkLessonCompletedAsync(
         int lessonId, string userId, CancellationToken cancellationToken = default);

    Task<Result<CourseProgressResponse>> GetCourseProgressAsync(
        string courseKeyId, string userId, CancellationToken cancellationToken = default);

    Task<Result<NextLessonResponse?>> GetNextLessonAsync(
        string courseKeyId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Internal call: invoked from ExamService when a user passes an exam.
    /// Idempotent — safe to call multiple times.
    /// </summary>
    Task MarkQuizLessonCompletedAsync(
        int lessonId, string userId, CancellationToken cancellationToken = default);
}
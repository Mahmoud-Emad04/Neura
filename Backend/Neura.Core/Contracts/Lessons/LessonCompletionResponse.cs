namespace Neura.Core.Contracts.Lessons;

public record LessonCompletionResponse(
    int LessonId,
    bool IsCompleted,
    DateTime? CompletedOn);

public record NextLessonResponse(
    int LessonId,
    int SectionId,
    string Title,
    string Type,
    int OrderIndex);

public record CourseProgressResponse(
    string CourseKeyId,
    int TotalLessons,
    int CompletedLessons,
    int ProgressPercentage,         // 0 - 100
    bool IsCourseCompleted,
    NextLessonResponse? NextLesson,
    List<int> CompletedLessonIds);
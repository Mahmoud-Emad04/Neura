namespace Neura.Core.Contracts.Lessons;

public record LessonResponse(
  int Id,
    string Title,
    string? Description,
    string Type,
    TimeSpan Duration,
    int OrderIndex,
    bool IsPreview,
    bool IsLocked,
    ExamPreviewInfo? Exam
);
public record ExamPreviewInfo(
    int TotalQuestions,
    int? DurationInMinutes,
    decimal PassingScorePercentage,
    int? MaxAttempts
);
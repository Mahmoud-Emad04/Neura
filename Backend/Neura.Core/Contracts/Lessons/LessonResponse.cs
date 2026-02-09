namespace Neura.Core.Contracts.Lessons;

public record LessonResponse(
    int Id,
    string Title,
    string? Description,
    string Type,

    string? VideoUrl,

    bool IsPreview,
    bool IsLocked,
    int OrderIndex,

    int? NextLessonId,
    int? PreviousLessonId
 );

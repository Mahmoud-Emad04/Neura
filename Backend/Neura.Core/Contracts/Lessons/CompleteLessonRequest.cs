namespace Neura.Core.Contracts.Lessons;

public record CompleteLessonRequest(
    string? Description,
    bool IsPreview,

    DateTime? ScheduledDate,

    IFormFile? VideoFile
);
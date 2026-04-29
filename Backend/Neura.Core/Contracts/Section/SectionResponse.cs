using Neura.Core.Contracts.Lessons;

namespace Neura.Core.Contracts.Section;

public record SectionResponse(
    int Id,
    string Title,
    string? Description,
    int Position,
    int TotalMinutes,
    int LessonsCount,
    List<LessonResponse>? Lessons
);
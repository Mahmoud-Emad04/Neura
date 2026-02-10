using Neura.Core.Contracts.Lessons;

namespace Neura.Core.Contracts.Section;

public record SectionResponse(
    int Id,
    string Title,
    string? Description,
    int Position,
    DateTime CreatedOn,
    DateTime? UpdatedOn,
    string CreatedById,
    string? UpdatedById,
    bool? IsDeleted,
    int TotalMinutes,
    List<LessonResponse>? Lessons
);
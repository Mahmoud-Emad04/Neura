namespace Neura.Core.Contracts.Section;

public record SectionResponse(
    string KeyId,
    string Title,
    string? Description,
    int Position,
    DateTime CreatedOn,
    DateTime? UpdatedOn,
    string CreatedById,
    string? UpdatedById,
    bool? IsDeleted
    //List<LessonResponse>? Lessons
);
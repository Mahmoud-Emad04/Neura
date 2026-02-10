using Neura.Core.Enums;

namespace Neura.Core.Contracts.Lessons;

public record CreateLessonRequest(
    string Title,
    int SectionId,
    LessonType Type
);
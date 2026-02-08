namespace Neura.Core.Contracts.Announcement;

public record PostRequest(
    string Title,
    string Content,
    bool IsPublic,
    int? CourseId,
    int? SectionId
);

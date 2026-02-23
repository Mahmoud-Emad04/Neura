namespace Neura.Core.Contracts.Announcement;

public record PostUpdateRequest(
    string Title,
    string Content,
    bool IsPublic
);
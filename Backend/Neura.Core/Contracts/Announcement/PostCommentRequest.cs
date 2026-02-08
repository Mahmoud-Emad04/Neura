namespace Neura.Core.Contracts.Announcement;

public record PostCommentRequest(
    string Content,
    int? ParentCommentId
);

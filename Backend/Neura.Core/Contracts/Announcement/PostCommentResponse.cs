namespace Neura.Core.Contracts.Announcement;

public record PostCommentResponse(
    int Id,
    int PostId,
    int? ParentCommentId,
    string Content,
    DateTime CreatedOn,
    DateTime? UpdatedOn,
    string CreatedById,
    string? UpdatedById,
    IEnumerable<PostCommentResponse> Replies
);

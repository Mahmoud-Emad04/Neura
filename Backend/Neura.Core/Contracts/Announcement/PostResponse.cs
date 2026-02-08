namespace Neura.Core.Contracts.Announcement;

public record PostResponse(
    int Id,
    string Title,
    string Content,
    bool IsPublic,
    int? CourseId,
    int? SectionId,
    int LikesCount,
    DateTime CreatedOn,
    DateTime? UpdatedOn,
    string CreatedById,
    string? UpdatedById,
    bool IsLikedByCurrentUser,
    IEnumerable<PostCommentResponse> Comments
);

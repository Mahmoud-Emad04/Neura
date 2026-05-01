namespace Neura.Core.Contracts.Announcement;

public record PostCommentResponse(
	int Id,
	int PostId,
	int? ParentCommentId,
	string Content,
	string? ImageUrl,
	DateTime CreatedOn,
	DateTime? UpdatedOn,
	string CreatedById,
	string CreatedByFullName,
	string? CreatedByImageUrl,
	string? UpdatedById,
	IEnumerable<PostCommentResponse> Replies
);
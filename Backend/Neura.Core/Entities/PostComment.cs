namespace Neura.Core.Entities;

public class PostComment : AuditableEntity
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;

    public Post Post { get; set; } = default!;
    public PostComment? ParentComment { get; set; }
    public ICollection<PostComment> Replies { get; set; } = new List<PostComment>();

    public DateTime? DeletedOn { get; set; }
    public string? DeletedById { get; set; }
}

namespace Neura.Core.Entities;

public class Post : AuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public int? CourseId { get; set; }
    public int? SectionId { get; set; }

    public Course? Course { get; set; }
    public Section? Section { get; set; }

    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();

    public DateTime? DeletedOn { get; set; }
    public string? DeletedById { get; set; }
}

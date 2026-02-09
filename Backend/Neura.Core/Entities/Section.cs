namespace Neura.Core.Entities;

public class Section : AuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Position { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public DateTime? DeletedOn { get; set; }
    public string? DeletedById { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = [];
}
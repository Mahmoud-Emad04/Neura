using Neura.Core.Entities;

public class Review : AuditableEntity
{
    public int CourseId { get; set; }
    public string UserId { get; set; } = default!;
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public Course Course { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
}
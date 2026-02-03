using Neura.Core.Entities;

public class CourseBookmark
{
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = default!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public bool IsDeleted { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
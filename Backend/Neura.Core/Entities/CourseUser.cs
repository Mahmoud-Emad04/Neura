using Neura.Core.Entities;

public class CourseUser
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public string UserId { get; set; } = null!;

    public ApplicationUser User { get; set; } = default!;
    public int PermissionsMask { get; set; }

    public bool IsDeleted { get; set; }
}
using Neura.Core.Entities;

public class CourseUser
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string UserId { get; set; } = string.Empty!;
    public ApplicationUser User { get; set; } = null!;

    public int PermissionMask { get; set; } = 0;
}

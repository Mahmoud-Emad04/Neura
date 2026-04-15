namespace Neura.Core.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? ColorHex { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? UpdatedById { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation
    public ICollection<Course> Courses { get; set; } = [];
}
namespace Neura.Core.Entities;

public sealed class Course : AuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly Startin { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly Endin { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public bool IsCompleted { get; set; } = false;
    public string ImageUrl { get; set; } = string.Empty;
    public int Price { get; set; }
    public double Rating { get; set; }
    public double TotalRatingSum { get; set; }
    public int TotalReviews { get; set; }

    public DateTime? DeletedOn { get; set; }
    public string? DeletedById { get; set; }

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<CourseUser> CourseUsers { get; set; } = new List<CourseUser>();
}
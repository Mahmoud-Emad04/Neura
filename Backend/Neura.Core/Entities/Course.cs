namespace Neura.Core.Entities;

public sealed class Course : AuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly Startin { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly Endin { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public bool IsCompleted { get; set; } = false;
    public string ImageUrl { get; set; } = string.Empty;


    public ICollection<Topic> Topics { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<CourseUser> CourseUsers { get; set; } = [];
}
namespace Neura.Core.Entities;

public class LessonCompletion
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;

    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = default!;

    public DateTime CompletedOn { get; set; } = DateTime.UtcNow;
}
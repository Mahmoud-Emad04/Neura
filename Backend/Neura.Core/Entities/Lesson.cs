using Neura.Core.Enums;

namespace Neura.Core.Entities;

public class Lesson : AuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid? VideoSortedName { get; set; }

    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    public int OrderIndex { get; set; }
    public bool IsPreview { get; set; } = false;
    public LessonType Type { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? ScheduledDate { get; set; }

    public int SectionId { get; set; }
    public Section Section { get; set; } = default!;
}

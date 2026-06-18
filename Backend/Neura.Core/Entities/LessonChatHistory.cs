using System;

namespace Neura.Core.Entities;

public class LessonChatHistory : AuditableEntity
{
    public int Id { get; set; }
    
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = default!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;

    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

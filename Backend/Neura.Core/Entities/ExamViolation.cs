namespace Neura.Core.Entities;

public class ExamViolation : AuditableEntity
{
    public int Id { get; set; }

    public int ExamId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public int? ExamAttemptId { get; set; }

    public string Severity { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }

    public bool CausedAutoSubmit { get; set; }
    public string? FrameImagePath { get; set; }

    // Navigation
    public Exam Exam { get; set; } = null!;
    public ApplicationUser Student { get; set; } = null!;
    public ExamAttempt? ExamAttempt { get; set; }
}
using Neura.Core.Enums;

namespace Neura.Core.Entities;

public class AttemptViolation
{
    public int Id { get; set; }
    public int ExamAttemptId { get; set; }

    public ViolationType ViolationType { get; set; }
    public DateTime OccurredAt { get; set; }

    // Navigation
    public ExamAttempt ExamAttempt { get; set; } = null!;
}
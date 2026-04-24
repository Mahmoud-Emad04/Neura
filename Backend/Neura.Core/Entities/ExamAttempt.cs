using Neura.Core.Enums;

namespace Neura.Core.Entities;

public class ExamAttempt
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public decimal? Score { get; set; }
    public decimal? ScorePercentage { get; set; }
    public bool? Passed { get; set; }
    public AttemptStatus Status { get; set; }

    public string QuestionOrder { get; set; } = string.Empty;
    public string AnswerOrder { get; set; } = string.Empty;

    // Navigation
    public Exam Exam { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();
    public ICollection<AttemptViolation> Violations { get; set; } = new List<AttemptViolation>();
}
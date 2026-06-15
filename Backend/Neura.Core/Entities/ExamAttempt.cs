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

    // ── Violation Workflow ──
    public decimal? OriginalScore { get; private set; }
    public decimal? FinalScore { get; private set; }
    public string? ViolationReason { get; private set; }
    public string? InstructorNotes { get; private set; }

    public string QuestionOrder { get; set; } = string.Empty;
    public string AnswerOrder { get; set; } = string.Empty;

    // Navigation
    public Exam Exam { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();
    public ICollection<AttemptViolation> Violations { get; set; } = new List<AttemptViolation>();

    // ── DDD Behavior ──

    public void FlagForViolation(string reason)
    {
        if (Status != AttemptStatus.Graded)
            throw new InvalidOperationException(
                "Only graded attempts can be flagged for violation.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Violation reason is required.", nameof(reason));

        OriginalScore = Score;
        ViolationReason = reason;
        Status = AttemptStatus.ViolationFlagged;
    }

    public void ResolveViolationAndOverrideGrade(decimal newScore, decimal totalPoints, string notes)
    {
        if (Status != AttemptStatus.ViolationFlagged)
            throw new InvalidOperationException(
                "Only violation-flagged attempts can be resolved.");

        if (string.IsNullOrWhiteSpace(notes))
            throw new ArgumentException("Instructor notes are required.", nameof(notes));

        FinalScore = newScore;
        Score = newScore;

        ScorePercentage = totalPoints > 0
            ? Math.Round((newScore / totalPoints) * 100, 2)
            : 0;

        if (Exam is not null)
            Passed = ScorePercentage >= Exam.PassingScorePercentage;

        InstructorNotes = notes;
        Status = AttemptStatus.Resolved;
    }
}
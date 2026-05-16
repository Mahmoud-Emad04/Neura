namespace Neura.Core.Contracts.ExamAttempt;

public class SubmitAttemptResponse
{
    public int AttemptId { get; set; }
    public decimal Score { get; set; }
    public decimal ScorePercentage { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal PassingScorePercentage { get; set; }
    public bool Passed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
}
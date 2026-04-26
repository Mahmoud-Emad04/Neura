namespace Neura.Core.Contracts.Analytics;


public class StudentAttemptSummaryResponse
{
    public int AttemptId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string? StudentEmail { get; set; }
    public decimal Score { get; set; }
    public decimal ScorePercentage { get; set; }
    public decimal TotalPoints { get; set; }
    public bool Passed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? DurationInSeconds { get; set; } // How long the student took
    public int ViolationCount { get; set; }
}
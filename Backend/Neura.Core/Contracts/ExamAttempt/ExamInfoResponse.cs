namespace Neura.Core.Contracts.ExamAttempt;

public class ExamInfoResponse
{
    public int ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DurationInMinutes { get; set; }
    public int QuestionCount { get; set; }         // Served count, NOT pool total
    public decimal TotalPoints { get; set; }
    public decimal PassingScorePercentage { get; set; }
    public int? MaxAttempts { get; set; }
    public int AttemptsTaken { get; set; }
    public int? RemainingAttempts { get; set; }     // null = unlimited
    public bool EnableTabSwitchDetection { get; set; }
    public int? MaxViolationsBeforeAutoSubmit { get; set; }
    public bool HasInProgressAttempt { get; set; }
    public int? InProgressAttemptId { get; set; }   // So frontend can resume
}
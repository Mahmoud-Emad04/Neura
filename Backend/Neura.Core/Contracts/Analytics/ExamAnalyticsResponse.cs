namespace Neura.Core.Contracts.Analytics;

public class ExamAnalyticsResponse
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;

    // ── Overview Stats ──
    public int TotalAttempts { get; set; }
    public int UniqueStudents { get; set; }
    public int CompletedAttempts { get; set; }
    public int InProgressAttempts { get; set; }
    public int TimedOutAttempts { get; set; }
    public int AutoSubmittedAttempts { get; set; }

    // ── Score Stats ──
    public decimal AverageScore { get; set; }
    public decimal AverageScorePercentage { get; set; }
    public decimal HighestScorePercentage { get; set; }
    public decimal LowestScorePercentage { get; set; }
    public decimal MedianScorePercentage { get; set; }

    // ── Pass/Fail ──
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal PassRate { get; set; } // Percentage

    // ── Violations ──
    public int TotalViolations { get; set; }
    public int StudentsWithViolations { get; set; }

    // ── Per-Question Breakdown ──
    public List<QuestionAnalyticsResponse> Questions { get; set; } = new();
}
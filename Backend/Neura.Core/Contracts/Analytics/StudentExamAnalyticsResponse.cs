namespace Neura.Core.Contracts.Analytics;

public class StudentExamAnalyticsResponse
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;

    // Student-specific Stats
    public decimal? StudentScorePercentage { get; set; }
    public int TotalStudentAttempts { get; set; }
    public bool HasCompletedAttempt { get; set; }

    // Class-wide Stats
    public decimal ClassAveragePercentage { get; set; }
    public decimal ClassHighestPercentage { get; set; }
    public decimal ClassMedianPercentage { get; set; }
    public int TotalClassAttempts { get; set; }
    
    // Rank/Percentile metrics
    public decimal? StudentPercentile { get; set; } 
}

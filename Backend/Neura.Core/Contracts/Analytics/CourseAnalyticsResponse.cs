namespace Neura.Core.Contracts.Analytics;

public class CourseAnalyticsResponse
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseStatus { get; set; } = string.Empty;

    // Echoed back so the client knows what range was applied
    public DateOnly? FilterFrom { get; set; }
    public DateOnly? FilterTo { get; set; }

    public EnrollmentAnalytics Enrollment { get; set; } = new();
    public ProgressAnalytics Progress { get; set; } = new();
    public ExamSummaryAnalytics Exams { get; set; } = new();
}

// ── 1. Enrollment ─────────────────────────────────────────────────────────────

public class EnrollmentAnalytics
{
    public DateOnly? FilterFrom { get; set; }
    public DateOnly? FilterTo { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }      // Accessed within filter range (or last 30 days if no filter)
    public int NewThisWeek { get; set; }         // Omitted / 0 when a custom range is applied
    public int NewThisMonth { get; set; }        // Omitted / 0 when a custom range is applied
    public List<DailyEnrollmentCount> EnrollmentTrend { get; set; } = new();
}

public class DailyEnrollmentCount
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

// ── 2. Student Progress ────────────────────────────────────────────────────────

public class ProgressAnalytics
{
    public DateOnly? FilterFrom { get; set; }
    public DateOnly? FilterTo { get; set; }
    public int TotalLessons { get; set; }
    public int PublishedLessons { get; set; }
    public decimal AverageCompletionPercentage { get; set; }
    public int StudentsCompleted100Percent { get; set; }
    public List<CompletionBucket> CompletionDistribution { get; set; } = new();
}

public class CompletionBucket
{
    public string Range { get; set; } = string.Empty;  // "0-25%", "26-50%", "51-75%", "76-99%", "100%"
    public int Count { get; set; }
}

// ── 3. Exam Performance ────────────────────────────────────────────────────────

public class ExamSummaryAnalytics
{
    public DateOnly? FilterFrom { get; set; }
    public DateOnly? FilterTo { get; set; }
    public int TotalExams { get; set; }
    public decimal OverallAverageScore { get; set; }
    public decimal OverallPassRate { get; set; }
    public List<ExamMiniSummary> PerExamSummary { get; set; } = new();
}

public class ExamMiniSummary
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public decimal AverageScorePercentage { get; set; }
    public decimal PassRate { get; set; }
}

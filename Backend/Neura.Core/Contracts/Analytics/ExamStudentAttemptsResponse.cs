namespace Neura.Core.Contracts.Analytics;

public class ExamStudentAttemptsResponse
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<StudentAttemptSummaryResponse> Attempts { get; set; } = new();
}
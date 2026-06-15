namespace Neura.Core.Contracts.ExamAttempt;

public class ExamViolationResponse
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string? StudentEmail { get; set; }
    public int? ExamAttemptId { get; set; }

    public string Severity { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }

    public bool CausedAutoSubmit { get; set; }
    public string? FrameImagePath { get; set; }

    public DateTime CreatedOn { get; set; }
}

public class ExamViolationsListResponse
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public List<ExamViolationResponse> Violations { get; set; } = new();
}

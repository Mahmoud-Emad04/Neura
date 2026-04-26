namespace Neura.Core.Contracts.ExamAttempt;

public class ViolationResponse
{
    public int TotalViolations { get; set; }
    public int? MaxViolationsBeforeAutoSubmit { get; set; }
    public bool AttemptAutoSubmitted { get; set; }
}
namespace Neura.Core.Contracts.ExamAttempt;

public class ResolveViolationRequest
{
    public decimal NewScore { get; set; }
    public string Notes { get; set; } = string.Empty;
}

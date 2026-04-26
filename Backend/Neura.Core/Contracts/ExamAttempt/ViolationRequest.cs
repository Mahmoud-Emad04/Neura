using Neura.Core.Enums;

namespace Neura.Core.Contracts.ExamAttempt;

public class ViolationRequest
{
    public ViolationType ViolationType { get; set; }
    public DateTime OccurredAt { get; set; }
}
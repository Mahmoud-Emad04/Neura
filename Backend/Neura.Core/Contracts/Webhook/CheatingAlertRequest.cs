namespace Neura.Core.Contracts.Webhook;

public class CheatingAlertRequest
{
    public string ExamId { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public double Timestamp { get; set; } // Unix epoch from Python time.time()

    public string Severity { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string? FrameData { get; set; } // Base64 encoded JPEG frame
}
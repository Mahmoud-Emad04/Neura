using System.Text.Json.Serialization;

namespace Neura.Core.Contracts.Webhook;

public class CheatingAlertRequest
{
    [JsonPropertyName("exam_id")]
    public string ExamId { get; set; } = string.Empty;

    [JsonPropertyName("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public double Timestamp { get; set; } // Unix epoch from Python time.time()

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("frame_data")]
    public string? FrameData { get; set; } // Base64 encoded JPEG frame
}
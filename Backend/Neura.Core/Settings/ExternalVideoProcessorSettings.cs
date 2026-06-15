namespace Neura.Core.Settings;

public class ExternalVideoProcessorSettings
{
    public const string SectionName = "ExternalVideoProcessor";
    public string BaseUrl { get; set; } = string.Empty;
    public string ProcessEndpoint { get; set; } = "/api/process";
    public string? ApiKey { get; set; }
}

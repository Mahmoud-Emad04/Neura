using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neura.Core.Services;
using Neura.Core.Settings;

namespace Neura.Services.Services;

public class ExternalVideoProcessorService : IExternalVideoProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalVideoProcessorService> _logger;
    private readonly ExternalVideoProcessorSettings _settings;

    public ExternalVideoProcessorService(HttpClient httpClient, ILogger<ExternalVideoProcessorService> logger, IOptions<ExternalVideoProcessorSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task ProcessVideoAsync(int lessonId, string downloadUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("Sending video for processing: LessonId={LessonId}", lessonId);

        var payload = new
        {
            lessonId = lessonId,
            downloadUrl = downloadUrl
        };

        var response = await _httpClient.PostAsJsonAsync(_settings.ProcessEndpoint, payload, ct);

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Successfully requested processing for LessonId={LessonId}", lessonId);
    }
}

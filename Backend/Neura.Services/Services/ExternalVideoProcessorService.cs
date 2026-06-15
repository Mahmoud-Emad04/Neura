using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Neura.Core.Services;

namespace Neura.Services.Services;

public class ExternalVideoProcessorService : IExternalVideoProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalVideoProcessorService> _logger;

    public ExternalVideoProcessorService(HttpClient httpClient, ILogger<ExternalVideoProcessorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ProcessVideoAsync(int lessonId, string downloadUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("Sending video for processing: LessonId={LessonId}", lessonId);

        var payload = new
        {
            lessonId = lessonId,
            downloadUrl = downloadUrl
        };

        var response = await _httpClient.PostAsJsonAsync("", payload, ct);

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Successfully requested processing for LessonId={LessonId}", lessonId);
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neura.Core.DTOs.Chatbot;
using Neura.Core.Services;
using Neura.Core.Settings;

namespace Neura.Services.Services;

public class ChatbotService : IChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatbotService> _logger;
    private readonly ChatbotSettings _settings;

    public ChatbotService(HttpClient httpClient, ILogger<ChatbotService> logger, IOptions<ChatbotSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<string> AskQuestionAsync(int lessonId, string question, List<ChatContextDto> history, CancellationToken ct = default)
    {
        _logger.LogInformation("Sending question to chatbot for LessonId={LessonId}", lessonId);

        var payload = new
        {
            lessonId = lessonId,
            question = question,
            history = history
        };

        var response = await _httpClient.PostAsJsonAsync(_settings.Endpoint, payload, ct);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(ct);

        try
        {
            // Try to parse as JSON first (e.g. { "answer": "..." })
            using var document = JsonDocument.Parse(responseContent);
            if (document.RootElement.TryGetProperty("answer", out var answerElement))
            {
                return answerElement.GetString() ?? responseContent;
            }
        }
        catch (JsonException)
        {
            // If it's not valid JSON, assume plain text response
        }

        return responseContent;
    }
}

using Microsoft.Extensions.Options;
using Neura.Core.DTOs.Chatbot;
using Neura.Core.Settings;
using System.Net.Http.Json;
using System.Text.Json;

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

    public async Task<string> AskQuestionAsync(string courseId, int lessonId, string question, string userRole, List<ChatContextDto> history, CancellationToken ct = default)
    {
        _logger.LogInformation("Sending question to chatbot for CourseId={CourseId}, LessonId={LessonId}", courseId, lessonId);

        var chatHistory = new List<object>();
        foreach (var item in history)
        {
            chatHistory.Add(new { role = userRole, content = item.Question });
            chatHistory.Add(new { role = "assistant", content = item.Answer });
        }

        var payload = new
        {
            chat_type = "course",
            chat_history = chatHistory,
            question = question,
            course_id = courseId,
            lesson_id = lessonId
        };

        var response = await _httpClient.PostAsJsonAsync(_settings.Endpoint, payload, ct);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(ct);

        try
        {
            // Try to parse as JSON first (e.g. { "answer": "..." })
            using var document = JsonDocument.Parse(responseContent);
            if (document.RootElement.TryGetProperty("reply", out var answerElement))
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

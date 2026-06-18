using Neura.Core.DTOs.Chatbot;

namespace Neura.Core.Services;

public interface IChatbotService
{
    Task<string> AskQuestionAsync(int lessonId, string question, List<ChatContextDto> history, CancellationToken ct = default);
}

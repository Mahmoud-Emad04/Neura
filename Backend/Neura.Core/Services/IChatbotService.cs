using Neura.Core.DTOs.Chatbot;

namespace Neura.Core.Services;

public interface IChatbotService
{
    Task<string> AskQuestionAsync(string courseId, int lessonId, string question, string userRole, List<ChatContextDto> history, CancellationToken ct = default);
}

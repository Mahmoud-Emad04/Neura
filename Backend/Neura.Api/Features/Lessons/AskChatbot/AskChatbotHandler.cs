using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.DTOs.Chatbot;
using Neura.Core.Entities;
using Neura.Core.Abstractions;
using Neura.Core.Services;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.AskChatbot;

internal sealed class AskChatbotHandler(ApplicationDbContext context, IChatbotService chatbotService, HashidsNet.IHashids hashids)
    : IRequestHandler<AskChatbotCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AskChatbotCommand command, CancellationToken ct)
    {
        // 1. Fetch lesson to ensure it exists and get CourseId
        var lesson = await context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct);
            
        if (lesson is null)
            return Result.Failure<string>(new Error("Lesson.NotFound", "The specified lesson was not found.", 404));

        var courseIdString = hashids.Encode(lesson.Section.CourseId);

        // 2. Retrieve last 2 chat history for context
        var pastHistory = await context.LessonChatHistories
            .AsNoTracking()
            .Where(h => h.LessonId == command.LessonId && h.UserId == command.UserId)
            .OrderByDescending(h => h.CreatedOn)
            .Take(2)
            .Select(h => new ChatContextDto
            {
                Question = h.Question,
                Answer = h.Answer
            })
            .ToListAsync(ct);

        // Reverse to have chronological order
        pastHistory.Reverse();

        // 3. Call Chatbot service
        string answer;
        try
        {
            answer = await chatbotService.AskQuestionAsync(courseIdString, command.LessonId, command.Question, command.UserRole, pastHistory, ct);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(new Error("Chatbot.Error", $"Failed to communicate with chatbot: {ex.Message}", 500));
        }

        // 4. Save the interaction in database
        var chatHistory = new LessonChatHistory
        {
            LessonId = command.LessonId,
            UserId = command.UserId,
            Question = command.Question,
            Answer = answer
        };

        context.LessonChatHistories.Add(chatHistory);
        await context.SaveChangesAsync(ct);

        // 5. Return answer
        return Result.Success(answer);
    }
}

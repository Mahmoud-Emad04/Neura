using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Lessons.AskChatbot;

public sealed record AskChatbotCommand(int LessonId, string Question, string UserId, string UserRole)
    : IRequest<Result<string>>;

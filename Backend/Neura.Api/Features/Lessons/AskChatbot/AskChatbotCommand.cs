using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Lessons.AskChatbot;

public sealed record AskChatbotCommand(int LessonId, string Question, string UserId)
    : IRequest<Result<string>>;

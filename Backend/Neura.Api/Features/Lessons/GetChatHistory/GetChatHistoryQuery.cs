using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.GetChatHistory;

public sealed record GetChatHistoryQuery(int LessonId, string UserId)
    : IRequest<Result<IEnumerable<ChatHistoryResponse>>>;

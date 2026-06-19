using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.GetChatHistory;

internal sealed class GetChatHistoryHandler(ApplicationDbContext context)
    : IRequestHandler<GetChatHistoryQuery, Result<IEnumerable<ChatHistoryResponse>>>
{
    public async Task<Result<IEnumerable<ChatHistoryResponse>>> Handle(GetChatHistoryQuery query, CancellationToken ct)
    {
        var history = await context.LessonChatHistories
            .AsNoTracking()
            .Where(h => h.LessonId == query.LessonId && h.UserId == query.UserId)
            .OrderBy(h => h.CreatedOn)
            .Select(h => new ChatHistoryResponse
            {
                Id = h.Id,
                Question = h.Question,
                Answer = h.Answer,
                CreatedOn = h.CreatedOn
            })
            .ToListAsync(ct);

        return Result.Success<IEnumerable<ChatHistoryResponse>>(history);
    }
}

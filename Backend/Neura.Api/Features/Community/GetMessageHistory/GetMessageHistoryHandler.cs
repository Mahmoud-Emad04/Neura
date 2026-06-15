using MediatR;
using Neura.Core.Contracts.Community;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Community.GetMessageHistory;

internal sealed class GetMessageHistoryHandler(ApplicationDbContext db)
    : IRequestHandler<GetMessageHistoryQuery, Result<PagedMessagesDto>>
{
    public async Task<Result<PagedMessagesDto>> Handle(
        GetMessageHistoryQuery request, CancellationToken ct)
    {
        var isMember = await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == request.ChannelId)
            .AnyAsync(c =>
                c.Course.CourseUsers.Any(cu =>
                    cu.UserId == request.UserId &&
                    !cu.IsDeleted),
                ct);

        if (!isMember)
            return Result.Failure<PagedMessagesDto>(new Error("Unauthorized", $"User {request.UserId} is not a member of the course owning channel {request.ChannelId}.", 403));

        var query = db.Messages
            .AsNoTracking()
            .Where(m => m.ChannelId == request.ChannelId);

        if (request.BeforeMessageId.HasValue)
            query = query.Where(m => m.Id < request.BeforeMessageId.Value);

        var rawMessages = await query
            .OrderByDescending(m => m.Id)
            .Take(request.PageSize + 1)
            .Select(m => new MessageDto(
                m.Id, m.ChannelId, m.SenderId,
                m.Sender.FirstName + " " + m.Sender.LastName,
                m.Sender.ImageUrl, m.Content, m.SentAt,
                m.EditedAt, m.IsDeleted, m.ReplyToMessageId, null))
            .ToListAsync(ct);

        var hasMore = rawMessages.Count > request.PageSize;
        if (hasMore) rawMessages.RemoveAt(rawMessages.Count - 1);

        var nextCursor = hasMore ? rawMessages[^1].Id : (long?)null;

        return Result.Success(new PagedMessagesDto(rawMessages.AsReadOnly(), nextCursor, hasMore));
    }
}

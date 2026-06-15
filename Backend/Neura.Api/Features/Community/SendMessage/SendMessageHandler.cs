using MediatR;
using Neura.Core.Contracts.Community;
using Neura.Core.Enums;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Community.SendMessage;

internal sealed class SendMessageHandler(ApplicationDbContext db)
    : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    public async Task<Result<MessageDto>> Handle(
        SendMessageCommand request, CancellationToken ct)
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
            return Result.Failure<MessageDto>(new Error("Unauthorized", $"User {request.UserId} is not a member of the course owning channel {request.ChannelId}.", 403));

        var channel = await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == request.ChannelId)
            .Select(c => new { c.Id, c.Type })
            .FirstOrDefaultAsync(ct);

        if (channel == null)
            return Result.Failure<MessageDto>(new Error("NotFound", $"Channel {request.ChannelId} not found.", 404));

        if (channel.Type != ChannelType.Text)
            return Result.Failure<MessageDto>(new Error("InvalidOperation", "Messages can only be sent to Text channels.", 400));

        var message = Message.Create(
            request.ChannelId, request.UserId, request.Content, request.ReplyToMessageId);

        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);

        return Result.Success(await db.Messages
            .AsNoTracking()
            .Where(m => m.Id == message.Id)
            .Select(m => new MessageDto(
                m.Id, m.ChannelId, m.SenderId,
                m.Sender.FirstName + " " + m.Sender.LastName,
                m.Sender.ImageUrl, m.Content, m.SentAt,
                m.EditedAt, m.IsDeleted, m.ReplyToMessageId, null))
            .FirstAsync(ct));
    }
}

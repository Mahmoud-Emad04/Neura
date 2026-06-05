namespace Neura.Api.Features.Community.SendMessage;

public sealed record SendMessageRequest(string Content, long? ReplyToMessageId);
public sealed record SendMessageResponse(long MessageId, DateTime SentAt);

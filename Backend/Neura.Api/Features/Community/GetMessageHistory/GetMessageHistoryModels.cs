namespace Neura.Api.Features.Community.GetMessageHistory;

public sealed record GetMessageHistoryRequest(long? Before, int PageSize = 50);

public sealed record MessageResponseViewModel(
    long Id, string SenderName, string? SenderAvatarUrl, string Content, DateTime SentAt, bool IsDeleted);

public sealed record PagedMessagesResponseViewModel(
    IReadOnlyList<MessageResponseViewModel> Messages, long? NextCursor, bool HasMore);

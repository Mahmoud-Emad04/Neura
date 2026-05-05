namespace Neura.Core.Contracts.Community;

public sealed record MessageDto(
    long Id,
    int ChannelId,
    string SenderId,
    string SenderName,
    string? SenderAvatarUrl,
    string Content,
    DateTime SentAt,
    DateTime? EditedAt,
    bool IsDeleted,
    long? ReplyToMessageId,

    /// <summary>
    ///     Populated only when ReplyToMessageId is not null.
    ///     A lightweight preview of the parent message so the client
    ///     can render the reply quote without a second fetch.
    /// </summary>
    ReplyPreviewDto? ReplyPreview = null
);

/// <summary>
///     Minimal snapshot of a replied-to message.
///     Avoids deep nesting (replies of replies of replies...).
/// </summary>
public sealed record ReplyPreviewDto(
    long Id,
    string SenderName,
    string ContentPreview    // First 100 chars of original content
);
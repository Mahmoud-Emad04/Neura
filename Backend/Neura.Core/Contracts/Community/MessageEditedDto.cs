namespace Neura.Core.Contracts.Community;

public sealed record MessageEditedDto(
    long Id,
    int ChannelId,
    string NewContent,
    DateTime EditedAt
);

/// <summary>
///     Broadcast to channel group when a message is soft-deleted.
///     Client replaces the message content with a tombstone UI element.
/// </summary>
public sealed record MessageDeletedDto(
    long Id,
    int ChannelId
);
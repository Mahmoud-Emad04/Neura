namespace Neura.Core.Contracts.Community;

public sealed record PagedMessagesDto(
    IReadOnlyList<MessageDto> Messages,

    /// <summary>
    ///     Pass this value as ?before= on the next request.
    ///     Null when HasMore = false.
    /// </summary>
    long? NextCursor,

    /// <summary>
    ///     False when this page contains the oldest messages in the channel.
    /// </summary>
    bool HasMore
);
using System.ComponentModel.DataAnnotations;

namespace Neura.Core.Contracts.Community;

public sealed record SendMessageHubRequest(
    [Range(1, int.MaxValue)]
    int    ChannelId,

    [Required, MinLength(1), MaxLength(4_000)]
    string Content,

    // Nullable — only populated when the user replies to a message
    long? ReplyToMessageId = null
);
using Neura.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Neura.Core.Contracts.Community;

/// <summary>
///     Request to create a new channel within a course.
///     POST /api/community/courses/{courseId}/channels
/// </summary>
public sealed record CreateChannelRequest(
    [Required, MinLength(1), MaxLength(100)]
    string Name,

    [MaxLength(1024)]
    string? Topic,

    [Required]
    ChannelType Type
);

/// <summary>
///     Request to update an existing channel's details.
///     PUT /api/community/channels/{channelId}
/// </summary>
public sealed record UpdateChannelRequest(
    [Required, MinLength(1), MaxLength(100)]
    string Name,

    [MaxLength(1024)]
    string? Topic
);

/// <summary>
///     Request to reorder channels within a course via drag-and-drop.
///     PUT /api/community/courses/{courseId}/channels/reorder
///
///     The client sends the complete ordered list of channel IDs.
///     The server assigns Position = index for each entry.
/// </summary>
public sealed record ReorderChannelsRequest(
    [Required, MinLength(1)]
    List<int> ChannelIds
);

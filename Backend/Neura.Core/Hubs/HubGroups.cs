namespace Neura.Core.Hubs;

/// <summary>
///     Group name generators for SignalR hybrid strategy.
///
/// ┌─────────────────────────────────────────────────────────────┐
/// │  course-{id}    → presence updates + unread notifications  │
/// │  channel-{id}   → full MessageDto payloads                │
/// │  voice-{id}     → voice channel participant events         │
/// └─────────────────────────────────────────────────────────────┘
/// </summary>
public static class HubGroups
{
    /// <summary>
    ///     Lightweight course-level group.
    ///     Receives: presence updates, unread badges, future announcements.
    ///     Does NOT receive: MessageDto payloads.
    ///     Joined: on SignalR connect (automatically).
    ///     Left:   on SignalR disconnect (automatically).
    /// </summary>
    public static string Course(int courseId)
        => $"course-{courseId}";

    /// <summary>
    ///     Heavy channel-level group.
    ///     Receives: full MessageDto payloads, typing indicators (future).
    ///     Joined: when user clicks a channel tab.
    ///     Left:   when user clicks a different channel tab or disconnects.
    /// </summary>
    public static string Channel(int channelId)
        => $"channel-{channelId}";

    /// <summary>
    ///     Voice-channel group for real-time participant events.
    ///     Joined: when user joins a voice channel.
    ///     Left:   when user leaves or is kicked.
    /// </summary>
    public static string VoiceChannel(int channelId)
        => $"voice-{channelId}";
}
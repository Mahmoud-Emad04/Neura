using Neura.Core.Contracts.Community;

namespace Neura.Core.Services;

public interface ICommunityHubClient
{
    // ── Presence ──────────────────────────────────────────────────────────
    Task PresenceChanged(PresenceUpdateDto update);
    Task InitialPresenceSync(IReadOnlyList<string> onlineUserIds);

    // ── Notifications ────────────────────────────────────────────────────
    Task UnreadNotification(UnreadNotificationDto notification);

    // ── Messages (broadcast to channel-{id} group) ───────────────────────
    Task ReceiveMessage(MessageDto message);
    Task MessageEdited(MessageEditedDto edit);
    Task MessageDeleted(MessageDeletedDto deleted);

    // ── Channels (broadcast to course-{id} group) ────────────────────────
    Task ChannelCreated(ChannelDto channel);
    Task ChannelUpdated(ChannelDto channel);
    Task ChannelDeleted(int channelId);

    // ── Errors (sent to caller only) ─────────────────────────────────────
    Task Error(string message);
}
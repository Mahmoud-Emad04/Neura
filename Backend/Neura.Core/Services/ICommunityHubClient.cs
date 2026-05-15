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

    // ── Channels (broadcast to course-{id} group) ───────────────────────
    Task ChannelCreated(ChannelDto channel);
    Task ChannelUpdated(ChannelDto channel);
    Task ChannelDeleted(int channelId);

    // ── Voice Channels (broadcast to voice-{id} group) ──────────────────
    Task VoiceParticipantJoined(VoiceParticipantDto participant);
    Task VoiceParticipantLeft(string userId);
    Task VoiceParticipantStateChanged(
        string userId,
        bool? isMuted,
        bool? isDeafened,
        bool? isSpeaking);

    /// <summary>
    ///     Sent only to the kicked user (caller-only).
    /// </summary>
    Task VoiceChannelKicked(string userId);

    /// <summary>
    ///     Initial sync sent to a caller joining a voice room —
    ///     full list of current participants so the UI can hydrate without an extra REST call.
    /// </summary>
    Task InitialVoiceRoomSync(IReadOnlyList<VoiceParticipantDto> participants);

    /// <summary>
    ///     WebRTC signaling relay — sent to a specific target connectionId.
    ///     Contains SDP offers, answers, or ICE candidates.
    /// </summary>
    Task WebRTCSignal(string senderId, object signal);

    // ── Errors (sent to caller only) ─────────────────────────────────────
    Task Error(string message);
}
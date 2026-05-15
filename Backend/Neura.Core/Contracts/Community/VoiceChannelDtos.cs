namespace Neura.Core.Contracts.Community;

/// <summary>
///     Current state of one participant in a voice channel.
///     Stored in the in-memory tracker (phase 1) or Redis (phase 2).
/// </summary>
public sealed record VoiceParticipantDto(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    bool IsMuted,
    bool IsDeafened,
    bool IsSpeaking,
    DateTime JoinedAt,
    string? ConnectionId = null
);

/// <summary>
///     SignalR request body for joining a voice channel.
/// </summary>
public sealed record JoinVoiceRequest(int ChannelId);

/// <summary>
///     SignalR request body for updating mute / deafen / speaking state.
///     All nullable — only changed fields are sent.
/// </summary>
public sealed record UpdateVoiceStateRequest(
    bool? IsMuted,
    bool? IsDeafened,
    bool? IsSpeaking
);
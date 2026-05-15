using Neura.Core.Contracts.Community;

namespace Neura.Core.Services;

/// <summary>
///     Owns all voice-channel participant operations.
///     Phase 1 (in-memory): <see cref="Services.VoiceChannelService"/>
///     Phase 2 (Redis):     <see cref="Helpers.RedisVoiceChannelTracker"/>
/// </summary>
public interface IVoiceChannelService
{
    /// <summary>
    ///     Adds the user to the voice channel room and returns their hydrated DTO.
    ///     Throws <see cref="UnauthorizedAccessException"/> — not a course member.
    ///     Throws <see cref="InvalidOperationException"/> — not a Voice channel, or user already in another.
    ///     Throws <see cref="KeyNotFoundException"/> — channel does not exist.
/// </summary>
    Task<VoiceParticipantDto> JoinVoiceAsync(string userId, string connectionId, int channelId);

    /// <summary>
    ///     Removes the user from whichever voice channel they are in.
    ///     Idempotent — safe to call even if not in any channel.
    /// </summary>
    Task LeaveVoiceAsync(string userId);

    /// <summary>
    ///     Updates mute / deafen / speaking state and returns the updated DTO.
    ///     Throws <see cref="UnauthorizedAccessException"/> — not a course member.
    ///     Throws <see cref="InvalidOperationException"/> — channel is not a Voice channel.
    ///     Throws <see cref="KeyNotFoundException"/> — channel does not exist.
/// </summary>
    Task<VoiceParticipantDto?> UpdateStateAsync(
        string userId, int channelId,
        bool? isMuted = null, bool? isDeafened = null, bool? isSpeaking = null);

    /// <summary>
    ///     Removes a target user from a voice channel.
    ///     Caller must have CoInstructor+ permission (same gate as channel management).
    ///     Throws <see cref="UnauthorizedAccessException"/> — caller lacks kick permission.
    ///     Throws <see cref="KeyNotFoundException"/> — channel does not exist.
/// </summary>
    Task<KickResult> KickAsync(string targetUserId, int channelId, string requestingUserId);

    /// <summary>
    ///     Returns the current participant list for a voice channel.
    ///     Throws <see cref="UnauthorizedAccessException"/> — not a course member.
    ///     Throws <see cref="InvalidOperationException"/> — not a Voice channel.
    ///     Throws <see cref="KeyNotFoundException"/> — channel does not exist.
/// </summary>
    Task<IReadOnlyList<VoiceParticipantDto>> GetParticipantsAsync(int channelId, string requestingUserId);

    /// <summary>Returns true if the user is currently in any voice channel.</summary>
    Task<bool> IsInVoiceChannelAsync(string userId);

    /// <summary>Returns the SignalR connectionId for a user in the voice room.</summary>
    string? GetConnectionId(string userId);

    /// <summary>
    ///     Returns the channelId the user is currently in, or null if not in any voice channel.
    /// </summary>
    int? GetUserCurrentChannelId(string userId);
}

/// <summary>
///     Result of a kick — carries the kicked user's id so the Hub can
///     broadcast to the correct SignalR client.
/// </summary>
public sealed record KickResult(string KickedUserId, int ChannelId);
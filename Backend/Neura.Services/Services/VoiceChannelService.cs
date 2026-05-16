using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Contracts.Community;
using Neura.Core.Enums;
using Neura.Core.Services;
using Neura.Repository.Persistence;

namespace Neura.Services.Services;

/// <summary>
///     In-memory voice channel participant tracker.
///     Phase 1 — replace with <see cref="Helpers.RedisVoiceChannelTracker"/> in production.
///
///     Two maps:
///     - _rooms: channelId → userId → <VoiceParticipantDto, connectionId>
///     - _userToChannel: userId → current channelId
/// </summary>
public sealed class VoiceChannelService(
    ApplicationDbContext db)
    : IVoiceChannelService
{
    // userId → (VoiceParticipantDto, connectionId)
    private static readonly ConcurrentDictionary<string, ParticipantEntry> _rooms = new();

    // userId → channelId they are currently in
    private static readonly ConcurrentDictionary<string, int> _userToChannel = new();

    public async Task<VoiceParticipantDto> JoinVoiceAsync(
        string userId,
        string connectionId,
        int channelId)
    {
        // ── 1. Verify channel ──────────────────────────────────────────────────
        var channel = await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == channelId)
            .Select(c => new { c.Id, c.CourseId, c.Type })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");

        if (channel.Type != ChannelType.Voice)
            throw new InvalidOperationException(
                "Only Voice channels can be joined via JoinVoiceAsync.");

        // ── 2. Security: course membership ────────────────────────────────────
        var isMember = await IsMemberAsync(userId, channel.CourseId);
        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User {userId} is not a member of course {channel.CourseId}.");

        // ── 3. Enforce one voice channel per user ────────────────────────────
        if (_userToChannel.TryGetValue(userId, out var existingChannelId)
            && existingChannelId != channelId)
        {
            throw new InvalidOperationException(
                $"User is already in voice channel {existingChannelId}. Leave it first.");
        }

        // ── 4. Load user profile ────────────────────────────────────────────────
        var profile = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName, u.ImageUrl })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var participant = new VoiceParticipantDto(
            UserId: userId,
            DisplayName: $"{profile.FirstName} {profile.LastName}".Trim(),
            AvatarUrl: profile.ImageUrl,
            IsMuted: false,
            IsDeafened: false,
            IsSpeaking: false,
            JoinedAt: DateTime.UtcNow,
            ConnectionId: connectionId);

        // ── 5. Insert into room ───────────────────────────────────────────────
        _rooms[userId] = new ParticipantEntry(participant, connectionId);
        _userToChannel[userId] = channelId;

        return participant;
    }

    public Task LeaveVoiceAsync(string userId)
    {
        RemoveParticipant(userId);
        return Task.CompletedTask;
    }

    public async Task<VoiceParticipantDto?> UpdateStateAsync(
        string userId,
        int channelId,
        bool? isMuted = null,
        bool? isDeafened = null,
        bool? isSpeaking = null)
    {
        // ── 1. Verify channel ────────────────────────────────────────────────
        var channel = await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == channelId)
            .Select(c => new { c.CourseId, c.Type })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");

        if (channel.Type != ChannelType.Voice)
            throw new InvalidOperationException("Channel is not a Voice channel.");

        // ── 2. Security ────────────────────────────────────────────────────────
        var isMember = await IsMemberAsync(userId, channel.CourseId);
        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User {userId} is not a member of this course.");

        // ── 3. Update ──────────────────────────────────────────────────────────
        if (!_rooms.TryGetValue(userId, out var entry)
            || entry.ConnectionId == default)
        {
            return null;
        }

        var current = entry.Participant;
        var updated = new VoiceParticipantDto(
            UserId: current.UserId,
            DisplayName: current.DisplayName,
            AvatarUrl: current.AvatarUrl,
            IsMuted: isMuted ?? current.IsMuted,
            IsDeafened: isDeafened ?? current.IsDeafened,
            IsSpeaking: isSpeaking ?? current.IsSpeaking,
            JoinedAt: current.JoinedAt,
            ConnectionId: current.ConnectionId);

        _rooms[userId] = entry with { Participant = updated };
        return updated;
    }

    public async Task<KickResult> KickAsync(
        string targetUserId,
        int channelId,
        string requestingUserId)
    {
        // ── 1. Verify channel ────────────────────────────────────────────────
        var channel = await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == channelId)
            .Select(c => new { c.CourseId })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");

        // ── 2. Permission gate ───────────────────────────────────────────────
        var hasPermission = await HasChannelManagementPermissionAsync(
            requestingUserId, channel.CourseId);
        if (!hasPermission)
            throw new UnauthorizedAccessException(
                $"User {requestingUserId} lacks permission to kick from channel {channelId}.");

        RemoveParticipant(targetUserId);
        return new KickResult(targetUserId, channelId);
    }

    public async Task<IReadOnlyList<VoiceParticipantDto>> GetParticipantsAsync(
        int channelId,
        string requestingUserId)
    {
        // ── 1. Verify channel ────────────────────────────────────────────────
        var channel = await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == channelId)
            .Select(c => new { c.CourseId, c.Type })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");

        if (channel.Type != ChannelType.Voice)
            throw new InvalidOperationException("Channel is not a Voice channel.");

        // ── 2. Security ────────────────────────────────────────────────────────
        var isMember = await IsMemberAsync(requestingUserId, channel.CourseId);
        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User {requestingUserId} is not a member of this course.");

        return _rooms.Values
            .Where(e => e.Participant.UserId != null)
            .Select(e => e.Participant)
            .OrderBy(p => p.DisplayName)
            .ToList()
            .AsReadOnly();
    }

    public Task<bool> IsInVoiceChannelAsync(string userId)
    {
        var inRoom = _rooms.ContainsKey(userId);
        return Task.FromResult(inRoom);
    }

    /// <summary>
    ///     Returns the connectionId for a user in the voice room,
    ///     so the hub can remove them from the SignalR group on kick/disconnect.
    /// </summary>
    public string? GetConnectionId(string userId)
    {
        return _rooms.TryGetValue(userId, out var entry)
            ? entry.ConnectionId
            : null;
    }

    public int? GetUserCurrentChannelId(string userId)
    {
        return _userToChannel.TryGetValue(userId, out var channelId)
            ? channelId
            : null;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<bool> IsMemberAsync(string userId, int courseId)
    {
        if (await IsAdminOrSuperAdminAsync(userId)) return true;
        return await db.CourseUsers.AsNoTracking().AnyAsync(cu =>
            cu.CourseId == courseId && cu.UserId == userId && !cu.IsDeleted);
    }

    private async Task<bool> HasChannelManagementPermissionAsync(string userId, int courseId)
    {
        if (await IsAdminOrSuperAdminAsync(userId)) return true;
        return await db.CourseUsers.AsNoTracking().AnyAsync(cu =>
            cu.CourseId == courseId && cu.UserId == userId &&
            cu.CourseRole.Level >= 3 && !cu.IsDeleted);
    }

    private async Task<bool> IsAdminOrSuperAdminAsync(string userId)
    {
        return await db.UserRoles.AsNoTracking().AnyAsync(ur =>
            ur.UserId == userId &&
            db.Roles.Where(r => r.Name == "Admin" || r.Name == "SuperAdmin")
                .Select(r => r.Id).Contains(ur.RoleId));
    }

    private static void RemoveParticipant(string userId)
    {
        _rooms.TryRemove(userId, out _);
        _userToChannel.TryRemove(userId, out _);
    }
}

/// <summary>
///     Internal record pairing a participant DTO with the SignalR connectionId
///     so the hub can remove the correct connection from the voice group.
/// </summary>
internal sealed record ParticipantEntry(
    VoiceParticipantDto Participant,
    string ConnectionId);
using StackExchange.Redis;
using Neura.Core.Contracts.Community;

namespace Neura.Services.Helpers;

/// <summary>
///     Redis-backed drop-in replacement for <see cref="VoiceChannelService"/>'s
///     in-memory participant storage.
///     Change ONE line in DI registration — zero service or hub changes.
///
///     Redis data structures:
///     1. Participant state hash: "voice:{channelId}:{userId}" → displayName, avatarUrl,
///        isMuted, isDeafened, isSpeaking, joinedAt
///     2. Channel member set: "vc-members:{channelId}" → SET of userIds
///     3. User → channel map: "vc-uc:{userId}" → channelId string, TTL 1 h
/// </summary>
public sealed class RedisVoiceChannelTracker(IConnectionMultiplexer redis)
    : IVoiceChannelTracker
{
    private readonly IDatabase _db = redis.GetDatabase();

    private const string VoiceKeyPrefix = "voice:";
    private const string MembersKeyPrefix = "vc-members:";
    private const string UserChannelKeyPrefix = "vc-uc:";
    private static readonly TimeSpan KeyTtl = TimeSpan.FromHours(1);

    public async Task<VoiceParticipantDto?> GetParticipantAsync(int channelId, string userId)
    {
        var hash = await _db.HashGetAllAsync(VoiceKey(channelId, userId));
        if (hash.Length == 0) return null;

        var d = hash.ToDictionary();
        return new VoiceParticipantDto(
            UserId: userId,
            DisplayName: d["displayName"].ToString()!,
            AvatarUrl: d.TryGetValue("avatarUrl", out var a) && !a.IsNullOrEmpty
                ? a.ToString()
                : null,
            IsMuted: bool.Parse(d["isMuted"].ToString()!),
            IsDeafened: bool.Parse(d["isDeafened"].ToString()!),
            IsSpeaking: bool.Parse(d["isSpeaking"].ToString()!),
            JoinedAt: DateTime.Parse(d["joinedAt"].ToString()!,
                System.Globalization.CultureInfo.InvariantCulture));
    }

    public async Task SetParticipantAsync(int channelId, VoiceParticipantDto participant)
    {
        var transaction = _db.CreateTransaction();

        _ = transaction.HashSetAsync(VoiceKey(channelId, participant.UserId), [
            new HashEntry("displayName", participant.DisplayName),
            new HashEntry("avatarUrl", participant.AvatarUrl ?? ""),
            new HashEntry("isMuted", participant.IsMuted.ToString()),
            new HashEntry("isDeafened", participant.IsDeafened.ToString()),
            new HashEntry("isSpeaking", participant.IsSpeaking.ToString()),
            new HashEntry("joinedAt", participant.JoinedAt.ToString(
                System.Globalization.CultureInfo.InvariantCulture))
        ]);
        _ = transaction.KeyExpireAsync(
            VoiceKey(channelId, participant.UserId), KeyTtl);
        _ = transaction.SetAddAsync(MembersKey(channelId), participant.UserId);
        _ = transaction.StringSetAsync(
            UserChannelKey(participant.UserId),
            channelId.ToString(),
            KeyTtl);

        await transaction.ExecuteAsync();
    }

    public async Task RemoveParticipantAsync(int channelId, string userId)
    {
        var transaction = _db.CreateTransaction();
        _ = transaction.KeyDeleteAsync(VoiceKey(channelId, userId));
        _ = transaction.SetRemoveAsync(MembersKey(channelId), userId);
        _ = transaction.KeyDeleteAsync(UserChannelKey(userId));
        await transaction.ExecuteAsync();

        var remaining = await _db.SetLengthAsync(MembersKey(channelId));
        if (remaining == 0)
            await _db.KeyDeleteAsync(MembersKey(channelId));
    }

    public async Task<IReadOnlyList<VoiceParticipantDto>> GetParticipantsAsync(int channelId)
    {
        var userIds = (await _db.SetMembersAsync(MembersKey(channelId)))
            .Select(v => v.ToString())
            .ToList();

        if (userIds.Count == 0)
            return [];

        var participants = new List<VoiceParticipantDto>(userIds.Count);
        foreach (var userId in userIds)
        {
            var p = await GetParticipantAsync(channelId, userId);
            if (p != null)
                participants.Add(p);
        }
        return participants.AsReadOnly();
    }

    public async Task<int?> GetUserCurrentChannelAsync(string userId)
    {
        var raw = await _db.StringGetAsync(UserChannelKey(userId));
        return raw.HasValue && int.TryParse(raw.ToString(), out var id) ? id : null;
    }

    private static string VoiceKey(int channelId, string userId)
        => $"{VoiceKeyPrefix}{channelId}:{userId}";

    private static string MembersKey(int channelId)
        => $"{MembersKeyPrefix}{channelId}";

    private static string UserChannelKey(string userId)
        => $"{UserChannelKeyPrefix}{userId}";
}

/// <summary>
///     Storage interface for voice channel participant state.
///     Separated from IVoiceChannelService so the tracker can be swapped
///     independently (in-memory vs Redis).
/// </summary>
public interface IVoiceChannelTracker
{
    Task<VoiceParticipantDto?> GetParticipantAsync(int channelId, string userId);
    Task SetParticipantAsync(int channelId, VoiceParticipantDto participant);
    Task RemoveParticipantAsync(int channelId, string userId);
    Task<IReadOnlyList<VoiceParticipantDto>> GetParticipantsAsync(int channelId);
    Task<int?> GetUserCurrentChannelAsync(string userId);
}
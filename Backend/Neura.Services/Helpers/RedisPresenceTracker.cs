using StackExchange.Redis;

namespace Neura.Services.Helpers;

/// <summary>
///     Redis-backed implementation of <see cref="IPresenceTracker"/>.
///     Drop-in replacement for <see cref="InMemoryPresenceTracker"/>.
///     Change ONE line in DI registration — zero Hub or Service changes.
///
///     Redis data structures used:
///
///     1. Connection metadata hash
///        Key:   "conn:{connectionId}"
///        Type:  Redis HASH
///        Fields: userId, courseId, channelId
///        TTL:   1 hour (safety net for orphaned keys on server crash)
///
///     2. User connection set
///        Key:   "presence:{userId}:{courseId}"
///        Type:  Redis SET of connectionIds
///        TTL:   24 hours
///
///     Why SET for connections?
///     SADD / SREM are O(1). SCARD (count members) is O(1).
///     Perfect for the multi-tab scenario — we SREM on disconnect
///     and check SCARD == 0 to detect last connection.
/// </summary>
public sealed class RedisPresenceTracker(IConnectionMultiplexer redis)
    : IPresenceTracker
{
    private readonly IDatabase _db = redis.GetDatabase();

    private const string ConnKeyPrefix = "conn:";
    private const string PresenceKeyPrefix = "presence:";
    private static readonly TimeSpan ConnTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan PresenceTtl = TimeSpan.FromHours(24);

    // -------------------------------------------------------------------------

    public async Task<bool> UserConnectedAsync(
        string userId,
        int courseId,
        string connectionId)
    {
        var connKey = ConnKeyPrefix + connectionId;
        var presenceKey = PresenceKey(userId, courseId);

        // MULTI/EXEC transaction — atomic: store metadata + add to presence set
        var transaction = _db.CreateTransaction();

        // Store connection metadata as a hash (userId + courseId + no channel yet)
        _ = transaction.HashSetAsync(connKey, [
            new HashEntry("userId",    userId),
            new HashEntry("courseId",  courseId.ToString()),
            new HashEntry("channelId", "")
        ]);
        _ = transaction.KeyExpireAsync(connKey, ConnTtl);

        // Add connectionId to the user's presence set for this course
        _ = transaction.SetAddAsync(presenceKey, connectionId);
        _ = transaction.KeyExpireAsync(presenceKey, PresenceTtl);

        await transaction.ExecuteAsync();

        // Check if this was the first connection (set had 0 members before SADD)
        // SCARD returns the current count AFTER our add
        var count = await _db.SetLengthAsync(presenceKey);
        return count == 1; // true = first connection = just came online
    }

    public async Task<DisconnectResult?> UserDisconnectedAsync(string connectionId)
    {
        var connKey = ConnKeyPrefix + connectionId;

        // Read metadata before deleting
        var fields = await _db.HashGetAllAsync(connKey);
        if (fields.Length == 0)
            return null; // Unknown connectionId — safe to ignore

        var userId = fields.FirstOrDefault(f => f.Name == "userId").Value.ToString();
        var courseId = int.Parse(fields.FirstOrDefault(f => f.Name == "courseId").Value!);
        var channelId = fields.FirstOrDefault(f => f.Name == "channelId").Value.ToString();

        var lastChannelId = string.IsNullOrEmpty(channelId)
            ? (int?)null
            : int.Parse(channelId);

        var presenceKey = PresenceKey(userId, courseId);

        // Atomic: delete metadata + remove from presence set
        var transaction = _db.CreateTransaction();
        _ = transaction.KeyDeleteAsync(connKey);
        _ = transaction.SetRemoveAsync(presenceKey, connectionId);
        await transaction.ExecuteAsync();

        // Check if they have remaining connections
        var remainingCount = await _db.SetLengthAsync(presenceKey);
        var userWentOffline = remainingCount == 0;

        if (userWentOffline)
            await _db.KeyDeleteAsync(presenceKey);

        return new DisconnectResult(
            UserId: userId,
            CourseId: courseId,
            LastChannelId: lastChannelId,
            UserWentOffline: userWentOffline);
    }

    public async Task<int?> UpdateCurrentChannelAsync(
        string connectionId,
        int? newChannelId)
    {
        var connKey = ConnKeyPrefix + connectionId;

        // Read previous channelId before overwriting
        var previousRaw = await _db.HashGetAsync(connKey, "channelId");
        int? previousChannelId = null;
        if (previousRaw.HasValue && !previousRaw.IsNullOrEmpty &&
            int.TryParse(previousRaw.ToString(), out var prev))
        {
            previousChannelId = prev;
        }

        // Write new channelId (or empty string to represent null)
        await _db.HashSetAsync(connKey, "channelId",
            newChannelId.HasValue ? newChannelId.Value.ToString() : "");

        return previousChannelId;
    }

    public async Task<IReadOnlyList<string>> GetOnlineUsersAsync(int courseId)
    {
        // Redis KEYS pattern scan — acceptable for moderate member counts.
        // For very large courses (1000+ concurrent), maintain a separate
        // "course:{courseId}:onlineUsers" SET updated on connect/disconnect.
        var server = redis.GetServer(redis.GetEndPoints().First());
        var pattern = $"{PresenceKeyPrefix}*:{courseId}";

        var userIds = new List<string>();

        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            var count = await _db.SetLengthAsync(key);
            if (count <= 0) continue;

            // Extract userId from "presence:{userId}:{courseId}"
            var keyStr = key.ToString();
            var parts = keyStr[PresenceKeyPrefix.Length..].Split(':');
            if (parts.Length >= 1)
                userIds.Add(parts[0]);
        }

        return userIds.AsReadOnly();
    }

    public async Task<bool> IsOnlineAsync(string userId, int courseId)
    {
        var count = await _db.SetLengthAsync(PresenceKey(userId, courseId));
        return count > 0;
    }

    // -------------------------------------------------------------------------

    private static string PresenceKey(string userId, int courseId)
        => $"{PresenceKeyPrefix}{userId}:{courseId}";
}
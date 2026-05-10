using System.Collections.Concurrent;

namespace Neura.Services.Services;

public sealed class InMemoryPresenceTracker : IPresenceTracker
{
    // "{userId}:{courseId}" → active connectionIds for that user in that course
    private readonly ConcurrentDictionary<string, HashSet<string>> _presenceMap = new();

    // connectionId → full metadata (userId, courseId, currentChannelId)
    private readonly ConcurrentDictionary<string, ConnectionMetadata> _connectionMeta = new();

    // Single semaphore — all mutations serialized (see class summary)
    private readonly SemaphoreSlim _lock = new(1, 1);

    // -------------------------------------------------------------------------
    // IPresenceTracker Implementation
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<bool> UserConnectedAsync(
        string userId,
        int courseId,
        string connectionId)
    {
        await _lock.WaitAsync();
        try
        {
            // Register metadata for O(1) lookup on future disconnect
            _connectionMeta[connectionId] = new ConnectionMetadata(
                UserId: userId,
                CourseId: courseId,
                CurrentChannelId: null);

            var key = PresenceKey(userId, courseId);

            if (!_presenceMap.TryGetValue(key, out var connections))
            {
                // Brand-new key — this is definitively the first connection
                _presenceMap[key] = [connectionId];
                return true; // ← User just came online → Hub must broadcast
            }

            var wasEmpty = connections.Count == 0;
            connections.Add(connectionId);

            // Only return true if the set was empty before (multi-tab: 2nd tab
            // opening should NOT trigger another "came online" broadcast)
            return wasEmpty;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<DisconnectResult?> UserDisconnectedAsync(string connectionId)
    {
        await _lock.WaitAsync();
        try
        {
            // Unknown connectionId — safe to ignore (e.g., duplicate disconnect events)
            if (!_connectionMeta.TryRemove(connectionId, out var meta))
                return null;

            var key = PresenceKey(meta.UserId, meta.CourseId);

            var userWentOffline = false;

            if (_presenceMap.TryGetValue(key, out var connections))
            {
                connections.Remove(connectionId);

                if (connections.Count == 0)
                {
                    // Last connection closed — user is now truly offline
                    _presenceMap.TryRemove(key, out _);
                    userWentOffline = true; // ← Hub must broadcast offline + write LastSeenAt to SQL
                }
            }

            return new DisconnectResult(
                UserId: meta.UserId,
                CourseId: meta.CourseId,
                LastChannelId: meta.CurrentChannelId,
                UserWentOffline: userWentOffline);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<int?> UpdateCurrentChannelAsync(
        string connectionId,
        int? newChannelId)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_connectionMeta.TryGetValue(connectionId, out var existing))
                return null;

            var previousChannelId = existing.CurrentChannelId;

            // Records are immutable — replace with updated copy
            _connectionMeta[connectionId] = existing with { CurrentChannelId = newChannelId };

            return previousChannelId;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetOnlineUsersAsync(int courseId)
    {
        // Must hold the lock because we read HashSet<string>.Count,
        // and HashSet is NOT thread-safe for concurrent reads + writes.
        // A concurrent UserConnectedAsync could mutate the set while we iterate.
        await _lock.WaitAsync();
        try
        {
            var suffix = $":{courseId}";

            return _presenceMap
                .Where(kvp => kvp.Key.EndsWith(suffix, StringComparison.Ordinal)
                           && kvp.Value.Count > 0)
                .Select(kvp => kvp.Key[..kvp.Key.LastIndexOf(':')]) // extract userId
                .ToList()
                .AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsOnlineAsync(string userId, int courseId)
    {
        await _lock.WaitAsync();
        try
        {
            var key = PresenceKey(userId, courseId);

            return _presenceMap.TryGetValue(key, out var connections)
                && connections.Count > 0;
        }
        finally
        {
            _lock.Release();
        }
    }

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Deterministic composite key.
    ///     Format: "{userId}:{courseId}" — colon chosen because userId (GUID string)
    ///     and courseId (int) will never contain a colon naturally.
    /// </summary>
    private static string PresenceKey(string userId, int courseId)
        => $"{userId}:{courseId}";
}

// -------------------------------------------------------------------------
// Private record — lives in the same file, not part of the public API surface
// -------------------------------------------------------------------------

/// <summary>
///     Everything we need to know about a connection
///     so we never have to query the DB on disconnect.
/// </summary>
internal sealed record ConnectionMetadata(
    string UserId,
    int CourseId,
    int? CurrentChannelId
);
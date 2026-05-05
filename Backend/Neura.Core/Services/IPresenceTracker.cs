namespace Neura.Core.Services;

/// <summary>
///     Manages real-time SignalR connection state entirely in memory.
///
///     Phase 1: <see cref="InMemoryPresenceTracker"/> (singleton, ConcurrentDictionary).
///     Phase 2: RedisPresenceTracker (same interface, Redis HSET/SREM under the hood).
///
///     ⚠️  This interface intentionally has NO knowledge of SQL Server.
///     The ONLY SQL write triggered by presence is <see cref="IChatService.PersistLastSeenAtAsync"/>,
///     which is called ONCE — when <see cref="UserDisconnectedAsync"/> returns
///     <see cref="DisconnectResult.UserWentOffline"/> = true.
/// </summary>
public interface IPresenceTracker
{
    /// <summary>
    ///     Registers a new SignalR connection for a user in a course.
    ///     Returns <c>true</c> if this is the user's FIRST active connection
    ///     to this course — i.e., they just came online and peers should be notified.
    /// </summary>
    Task<bool> UserConnectedAsync(string userId, int courseId, string connectionId);

    /// <summary>
    ///     Removes a SignalR connection.
    ///     Returns a <see cref="DisconnectResult"/> containing everything the Hub needs
    ///     to perform group cleanup and presence broadcasts — with zero extra lookups.
    ///     Returns <c>null</c> if the connectionId was never registered (safe to ignore).
    /// </summary>
    Task<DisconnectResult?> UserDisconnectedAsync(string connectionId);

    /// <summary>
    ///     Updates which channel a specific connection is currently viewing.
    ///     Returns the PREVIOUS channelId so the Hub can remove the connection
    ///     from the old <c>channel-{id}</c> group before joining the new one.
    ///     Returns <c>null</c> if the connection wasn't viewing any channel.
    /// </summary>
    Task<int?> UpdateCurrentChannelAsync(string connectionId, int? newChannelId);

    /// <summary>
    ///     Returns all distinct user IDs with at least one active connection in a course.
    ///     Used to hydrate the online member list sidebar on initial page load.
    /// </summary>
    Task<IReadOnlyList<string>> GetOnlineUsersAsync(int courseId);

    /// <summary>
    ///     Returns <c>true</c> if the user has at least one active connection to this course.
    /// </summary>
    Task<bool> IsOnlineAsync(string userId, int courseId);
}

/// <summary>
///     The result of a user disconnecting.
///     Carries all context the Hub needs — no secondary lookups required after disconnect.
/// </summary>
/// <param name="UserId">The user who disconnected.</param>
/// <param name="CourseId">The course this connection was associated with.</param>
/// <param name="LastChannelId">
///     The channel they were viewing, if any. Used to remove them from
///     the <c>channel-{id}</c> SignalR group on disconnect.
/// </param>
/// <param name="UserWentOffline">
///     <c>true</c> = this was the last connection for this user+course combo.
///     The Hub must broadcast offline status and trigger the SQL LastSeenAt write.
/// </param>
public sealed record DisconnectResult(
    string UserId,
    int CourseId,
    int? LastChannelId,
    bool UserWentOffline
);
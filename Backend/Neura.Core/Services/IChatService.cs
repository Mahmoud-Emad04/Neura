using Neura.Core.Contracts.Community;

namespace Neura.Core.Services;

/// <summary>
///     Owns ALL persistence and query operations for the community feature.
///
///     Responsibilities:
///     ├── Message CRUD (create, edit, soft-delete)
///     ├── Cursor-based message history pagination
///     ├── Channel queries (sidebar list)
///     ├── Membership validation (Hub security gate)
///     └── Presence persistence (LastSeenAt on ApplicationUser — written once
///         on final disconnect, never on every ping)
///
///     ⚠️  Neither the Hub nor Controllers reference DbContext directly.
///     This interface is the ONLY persistence surface they touch.
/// </summary>
public interface IChatService
{
    // =========================================================================
    // Message Operations
    // =========================================================================

    /// <summary>
    ///     Validates membership, persists the message, and returns a
    ///     fully-hydrated DTO ready for SignalR broadcast.
    ///
    ///     Throws <see cref="UnauthorizedAccessException"/>
    ///         → user is not a member of the course owning this channel.
    ///     Throws <see cref="KeyNotFoundException"/>
    ///         → channel does not exist.
    ///     Throws <see cref="InvalidOperationException"/>
    ///         → channel is not a Text channel.
    /// </summary>
    Task<MessageDto> SaveMessageAsync(
        int channelId,
        string senderId,
        string content,
        long? replyToMessageId = null,
        CancellationToken ct = default);

    /// <summary>
    ///     Cursor-based paginated message history for a channel.
    ///
    ///     Pass <paramref name="beforeMessageId"/> = null → load the latest page.
    ///     Pass the returned <see cref="PagedMessagesDto.NextCursor"/> → load older messages.
    ///     Stop when <see cref="PagedMessagesDto.HasMore"/> = false.
    ///
    ///     Results: descending order (newest → oldest). Client reverses for display.
    ///
    ///     Throws <see cref="UnauthorizedAccessException"/>
    ///         → user is not a member of the course owning this channel.
    /// </summary>
    Task<PagedMessagesDto> GetMessageHistoryAsync(
        int channelId,
        string requestingUserId,
        long? beforeMessageId = null,
        int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>
    ///     Edits message content and stamps EditedAt.
    ///     Only the original sender may edit.
    ///
    ///     Throws <see cref="UnauthorizedAccessException"/>
    ///         → requestingUserId is not the original sender.
    ///     Throws <see cref="KeyNotFoundException"/>
    ///         → message does not exist.
    ///     Throws <see cref="InvalidOperationException"/>
    ///         → message is already soft-deleted.
    /// </summary>
    Task<MessageEditedDto> EditMessageAsync(
        long messageId,
        string requestingUserId,
        string newContent,
        CancellationToken ct = default);

    /// <summary>
    ///     Soft-deletes a message and replaces content with a tombstone.
    ///     Sender OR a course member with role Level >= 3 (CoInstructor+) may delete.
    ///
    ///     Throws <see cref="UnauthorizedAccessException"/>
    ///         → requestingUserId has no permission to delete.
    ///     Throws <see cref="KeyNotFoundException"/>
    ///         → message does not exist.
    /// </summary>
    Task<MessageDeletedDto> DeleteMessageAsync(
        long messageId,
        string requestingUserId,
        CancellationToken ct = default);

    // =========================================================================
    // Channel Operations
    // =========================================================================

    /// <summary>
    ///     Returns all visible, non-deleted channels for a course ordered by Position ASC.
    ///     Leverages IX_Channels_CourseId_Position composite index.
    ///
    ///     Throws <see cref="UnauthorizedAccessException"/>
    ///         → user is not a member of the course.
    /// </summary>
    Task<IReadOnlyList<ChannelDto>> GetChannelsAsync(
        int courseId,
        string requestingUserId,
        CancellationToken ct = default);

    /// <summary>
    ///     Returns the CourseId that owns the given channel.
    ///     Used by the Hub to resolve the course-{id} group for
    ///     UnreadNotification broadcast after a message is sent.
    ///
    ///     Throws <see cref="KeyNotFoundException"/>
    ///         → channel does not exist.
    /// </summary>
    Task<int> GetCourseIdForChannelAsync(
        int channelId,
        CancellationToken ct = default);

    // =========================================================================
    // Membership & Security
    // =========================================================================

    /// <summary>
    ///     Returns true if the user is an active (non-deleted) member
    ///     of the course that owns the given channel.
    ///     This is the security gate called by the Hub before
    ///     JoinChannel and SendMessage.
    /// </summary>
    Task<bool> IsCourseMemberAsync(
        string userId,
        int channelId,
        CancellationToken ct = default);

    /// <summary>
    ///     Returns all active members of a course with their real-time
    ///     online status (from IPresenceTracker) and LastSeenAt (from
    ///     ApplicationUser — no extra join needed).
    ///
    ///     Throws <see cref="UnauthorizedAccessException"/>
    ///         → requestingUserId is not a member of the course.
    /// </summary>
    Task<IReadOnlyList<CourseMemberDto>> GetCourseMembersAsync(
        int courseId,
        string requestingUserId,
        CancellationToken ct = default);

    // =========================================================================
    // Presence Persistence
    // =========================================================================

    /// <summary>
    ///     Stamps ApplicationUser.LastSeenAt = UtcNow via a single
    ///     ExecuteUpdateAsync call (no entity tracking, no SELECT round-trip).
    ///
    ///     Called ONCE — only when IPresenceTracker.UserDisconnectedAsync
    ///     returns UserWentOffline = true (i.e., all tabs are closed).
    ///     Never called on every SignalR ping or tab switch.
    /// </summary>
    Task PersistLastSeenAtAsync(
        string userId,
        CancellationToken ct = default);    // ← courseId parameter REMOVED
}
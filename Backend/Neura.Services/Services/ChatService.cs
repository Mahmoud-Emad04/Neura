using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Community;
using Neura.Core.Enums;
using System.Linq.Expressions;

namespace Infrastructure.Services.Community;

/// <summary>
///     Concrete implementation of <see cref="IChatService"/>.
///
///     EF Core performance rules applied throughout:
///     ✅ AsNoTracking()       — every read query (zero change-tracking overhead)
///     ✅ Select projection    — never materialize full entity graphs for DTO responses
///     ✅ ExecuteUpdateAsync() — single-statement UPDATE with no fetch round-trip
///     ✅ Cursor pagination    — WHERE Id &lt; @cursor + ORDER BY Id DESC (index-driven)
///     ✅ pageSize + 1 trick   — detects HasMore without a COUNT(*) query
///     ✅ No lazy loading      — all related data resolved in explicit projections
/// </summary>
public sealed class ChatService(
    ApplicationDbContext db,
    IPresenceTracker presenceTracker)
    : IChatService
{
    // =========================================================================
    // Message Operations
    // =========================================================================

    /// <inheritdoc/>
    public async Task<MessageDto> SaveMessageAsync(
        int channelId,
        string senderId,
        string content,
        long? replyToMessageId = null,
        CancellationToken ct = default)
    {
        // ── 1. Security ───────────────────────────────────────────────────────
        var isMember = await IsCourseMemberAsync(senderId, channelId, ct);
        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User {senderId} is not a member of the course owning channel {channelId}.");

        // ── 2. Validate channel (type check included) ─────────────────────────
        // AsNoTracking + projection: we only need 3 fields, not the full entity
        var channel = await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == channelId)
            .Select(c => new { c.Id, c.CourseId, c.Type })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");

        if (channel.Type != ChannelType.Text)
            throw new InvalidOperationException(
                "Messages can only be sent to Text channels.");

        // ── 3. Persist ────────────────────────────────────────────────────────
        var message = Message.Create(channelId, senderId, content, replyToMessageId);
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);

        // ── 4. Return hydrated DTO (reuses shared projection) ─────────────────
        // Separate query after insert so EF Core can resolve the Sender
        // navigation join cleanly without tracking graph complexity.
        return await db.Messages
            .AsNoTracking()
            .Where(m => m.Id == message.Id)
            .Select(MessageProjection)
            .FirstAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<PagedMessagesDto> GetMessageHistoryAsync(
        int channelId,
        string requestingUserId,
        long? beforeMessageId = null,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        // ── 1. Security ───────────────────────────────────────────────────────
        var isMember = await IsCourseMemberAsync(requestingUserId, channelId, ct);
        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User {requestingUserId} is not a member of the course owning channel {channelId}.");

        // ── 2. Clamp pageSize (prevent abuse) ─────────────────────────────────
        pageSize = Math.Clamp(pageSize, 1, 100);

        // ── 3. Build cursor query ─────────────────────────────────────────────
        // Leverages IX_Messages_ChannelId_Id_Desc composite index.
        // HasQueryFilter on Message already excludes soft-deleted rows
        // UNLESS IgnoreQueryFilters() is explicitly called.
        var query = db.Messages
            .AsNoTracking()
            .Where(m => m.ChannelId == channelId);

        if (beforeMessageId.HasValue)
            query = query.Where(m => m.Id < beforeMessageId.Value);

        // ── 4. Fetch pageSize + 1 rows ────────────────────────────────────────
        // The +1 row acts as a sentinel: if we get it back, more rows exist.
        // We slice it off before returning. This avoids a separate COUNT(*) query.
        var rawMessages = await query
            .OrderByDescending(m => m.Id)
            .Take(pageSize + 1)
            .Select(MessageProjection)
            .ToListAsync(ct);

        // ── 5. Determine HasMore + NextCursor ─────────────────────────────────
        var hasMore = rawMessages.Count > pageSize;

        if (hasMore)
            rawMessages.RemoveAt(rawMessages.Count - 1);    // drop the sentinel

        var nextCursor = hasMore
            ? rawMessages[^1].Id        // smallest Id in this page
            : (long?)null;

        return new PagedMessagesDto(
            Messages: rawMessages.AsReadOnly(),
            NextCursor: nextCursor,
            HasMore: hasMore);
    }

    /// <inheritdoc/>
    public async Task<MessageEditedDto> EditMessageAsync(
        long messageId,
        string requestingUserId,
        string newContent,
        CancellationToken ct = default)
    {
        // Tracking query intentional here — we need EF to persist the mutation
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct)
            ?? throw new KeyNotFoundException($"Message {messageId} not found.");

        if (message.SenderId != requestingUserId)
            throw new UnauthorizedAccessException(
                "Only the original sender can edit this message.");

        // Domain method: updates Content + stamps EditedAt atomically
        message.Edit(newContent);
        await db.SaveChangesAsync(ct);

        return new MessageEditedDto(
            Id: message.Id,
            ChannelId: message.ChannelId,
            NewContent: message.Content,
            EditedAt: message.EditedAt!.Value);
    }

    /// <inheritdoc/>
    public async Task<MessageDeletedDto> DeleteMessageAsync(
        long messageId,
        string requestingUserId,
        CancellationToken ct = default)
    {
        // Tracking query intentional — we need to mutate and save
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct)
            ?? throw new KeyNotFoundException($"Message {messageId} not found.");

        // ── Permission check: sender OR course admin (Level >= 3) ─────────────
        var isSender = message.SenderId == requestingUserId;

        if (!isSender)
        {
            // Resolve courseId from the channel in a clean separate query
            var channelCourseId = await db.Channels
                .AsNoTracking()
                .Where(c => c.Id == message.ChannelId)
                .Select(c => c.CourseId)
                .FirstAsync(ct);

            var isAdmin = await IsAdminOrSuperAdminAsync(requestingUserId, ct);

            var isCourseAdmin = !isAdmin && await db.CourseUsers
                .AsNoTracking()
                .AnyAsync(cu =>
                    cu.UserId == requestingUserId &&
                    cu.CourseId == channelCourseId &&
                    cu.CourseRole.Level >= 3 &&
                    !cu.IsDeleted,
                    ct);

            if (!isAdmin && !isCourseAdmin)
                throw new UnauthorizedAccessException(
                    "Only the sender or a course admin can delete this message.");
        }

        // Tombstone content, then soft-delete via domain methods
        message.Edit("[message deleted]");
        message.SoftDelete();
        await db.SaveChangesAsync(ct);

        return new MessageDeletedDto(
            Id: message.Id,
            ChannelId: message.ChannelId);
    }

    // =========================================================================
    // Channel Operations
    // =========================================================================

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChannelDto>> GetChannelsAsync(
        int courseId,
        string requestingUserId,
        CancellationToken ct = default)
    {
        // ── Security ──────────────────────────────────────────────────────────
        var isMember = await IsCourseMemberByIdAsync(requestingUserId, courseId, ct);
        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User {requestingUserId} is not a member of course {courseId}.");

        // Uses IX_Channels_CourseId_Position composite index.
        // HasQueryFilter automatically excludes IsDeleted channels.
        var channels = await db.Channels
            .AsNoTracking()
            .Where(c => c.CourseId == courseId)
            .OrderBy(c => c.Position)
            .Select(c => new ChannelDto(
                c.Id,
                c.Name,
                c.Topic,
                c.Type,
                c.Position))
            .ToListAsync(ct);

        return channels.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<ChannelDto> CreateChannelAsync(
        int courseId,
        string requestingUserId,
        string name,
        ChannelType type,
        string? topic = null,
        CancellationToken ct = default)
    {
        // ── Security: CoInstructor+ or platform Admin ─────────────────────────
        await EnsureChannelManagementPermissionAsync(requestingUserId, courseId, ct);

        // Verify the course exists
        var courseExists = await db.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            throw new KeyNotFoundException($"Course {courseId} not found.");

        // Auto-assign position to the end of the current channel list
        var maxPosition = await db.Channels
            .AsNoTracking()
            .Where(c => c.CourseId == courseId)
            .Select(c => (int?)c.Position)
            .MaxAsync(ct) ?? -1;

        var channel = Channel.Create(courseId, name, type, maxPosition + 1, topic);
        db.Channels.Add(channel);
        await db.SaveChangesAsync(ct);

        return new ChannelDto(
            channel.Id,
            channel.Name,
            channel.Topic,
            channel.Type,
            channel.Position);
    }

    /// <inheritdoc/>
    public async Task<ChannelDto> UpdateChannelAsync(
        int channelId,
        string requestingUserId,
        string name,
        string? topic,
        CancellationToken ct = default)
    {
        // Tracking query intentional — we need EF to persist the mutation
        var channel = await db.Channels
            .FirstOrDefaultAsync(c => c.Id == channelId, ct)
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");

        // ── Security: CoInstructor+ or platform Admin ─────────────────────────
        await EnsureChannelManagementPermissionAsync(requestingUserId, channel.CourseId, ct);

        // Domain method handles trim + lowercase
        channel.UpdateDetails(name, topic);
        await db.SaveChangesAsync(ct);

        return new ChannelDto(
            channel.Id,
            channel.Name,
            channel.Topic,
            channel.Type,
            channel.Position);
    }

    /// <inheritdoc/>
    public async Task<(int ChannelId, int CourseId)> DeleteChannelAsync(
        int channelId,
        string requestingUserId,
        CancellationToken ct = default)
    {
        // Tracking query intentional — we need to mutate and save
        var channel = await db.Channels
            .FirstOrDefaultAsync(c => c.Id == channelId, ct)
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");

        // ── Security: CoInstructor+ or platform Admin ─────────────────────────
        await EnsureChannelManagementPermissionAsync(requestingUserId, channel.CourseId, ct);

        // Domain method sets IsDeleted = true (soft delete)
        channel.Delete();
        await db.SaveChangesAsync(ct);

        return (channel.Id, channel.CourseId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChannelDto>> ReorderChannelsAsync(
        int courseId,
        string requestingUserId,
        List<int> channelIds,
        CancellationToken ct = default)
    {
        // ── Security: CoInstructor+ or platform Admin ─────────────────────────
        await EnsureChannelManagementPermissionAsync(requestingUserId, courseId, ct);

        // Load all non-deleted channels for this course (tracking enabled for mutation)
        var channels = await db.Channels
            .Where(c => c.CourseId == courseId)
            .ToListAsync(ct);

        // Validate: the provided list must contain exactly the same IDs
        var existingIds = channels.Select(c => c.Id).OrderBy(id => id).ToList();
        var providedIds = channelIds.Distinct().OrderBy(id => id).ToList();

        if (!existingIds.SequenceEqual(providedIds))
            throw new InvalidOperationException(
                "The provided channel IDs must match exactly the existing channels in this course.");

        // Build a lookup and assign Position = index from the ordered list
        var lookup = channels.ToDictionary(c => c.Id);
        for (var i = 0; i < channelIds.Count; i++)
        {
            lookup[channelIds[i]].Reorder(i);
        }

        await db.SaveChangesAsync(ct);

        // Return the reordered list
        return channels
            .OrderBy(c => c.Position)
            .Select(c => new ChannelDto(
                c.Id,
                c.Name,
                c.Topic,
                c.Type,
                c.Position))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<int> GetCourseIdForChannelAsync(
        int channelId,
        CancellationToken ct = default)
    {
        var courseId = await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == channelId)
            .Select(c => (int?)c.CourseId)
            .FirstOrDefaultAsync(ct);

        return courseId
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");
    }

    // =========================================================================
    // Membership & Security
    // =========================================================================

    /// <inheritdoc/>
    public async Task<bool> IsCourseMemberAsync(
        string userId,
        int channelId,
        CancellationToken ct = default)
    {
        // Admin/SuperAdmin bypass — platform admins can access any channel
        if (await IsAdminOrSuperAdminAsync(userId, ct))
            return true;

        // Single-query join: channel → course → courseUsers
        // No in-memory filtering. Fully translatable to SQL.
        return await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == channelId)
            .AnyAsync(c =>
                c.Course.CourseUsers.Any(cu =>
                    cu.UserId == userId &&
                    !cu.IsDeleted),
                ct);
    }

    /// <inheritdoc/>
    public async Task<bool> IsCourseMemberByIdAsync(
        string userId,
        int courseId,
        CancellationToken ct = default)
    {
        // Admin/SuperAdmin bypass — platform admins can access any course
        if (await IsAdminOrSuperAdminAsync(userId, ct))
            return true;

        return await db.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == userId &&
                !cu.IsDeleted,
                ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CourseMemberDto>> GetCourseMembersAsync(
        int courseId,
        string requestingUserId,
        CancellationToken ct = default)
    {
        // ── Security ──────────────────────────────────────────────────────────
        var isMember = await IsCourseMemberByIdAsync(requestingUserId, courseId, ct);
        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User {requestingUserId} is not a member of course {courseId}.");

        // ── Single SQL query: member list with LastSeenAt included ────────────
        // LastSeenAt now lives directly on ApplicationUser — zero extra join
        // vs the old UserPresence table which required a separate LEFT JOIN.
        var members = await db.CourseUsers
            .AsNoTracking()
            .Where(cu => cu.CourseId == courseId && !cu.IsDeleted)
            .Select(cu => new
            {
                cu.UserId,
                DisplayName = cu.User.FirstName + " " + cu.User.LastName,
                AvatarUrl = cu.User.ImageUrl,
                RoleName = cu.CourseRole.Name,
                LastSeenAt = cu.User.LastSeenAt   // ✅ Direct column — no join
            })
            .ToListAsync(ct);

        // ── Hydrate IsOnline from IPresenceTracker (pure in-memory) ──────────
        // Task.WhenAll runs all IsOnlineAsync checks concurrently.
        // IPresenceTracker has no async I/O in Phase 1, so this
        // is effectively synchronous — but the interface is already
        // Redis-ready for Phase 2 where these WOULD be real async calls.
        var dtos = await Task.WhenAll(
            members.Select(async m => new CourseMemberDto(
                UserId: m.UserId,
                DisplayName: m.DisplayName,
                AvatarUrl: m.AvatarUrl,
                RoleName: m.RoleName,
                IsOnline: await presenceTracker.IsOnlineAsync(m.UserId, courseId),
                LastSeenAt: m.LastSeenAt          // ✅ No secondary SQL lookup
            )));

        return dtos.AsReadOnly();
    }

    // =========================================================================
    // Presence Persistence
    // =========================================================================

    /// <inheritdoc/>
    public async Task PersistLastSeenAtAsync(
        string userId,
        CancellationToken ct = default)
    {
        // Single UPDATE statement — no SELECT, no entity tracking, no SaveChanges.
        // EF Core translates ExecuteUpdateAsync directly to:
        //
        //   UPDATE AspNetUsers
        //   SET LastSeenAt = @now
        //   WHERE Id = @userId
        //
        // This is the most efficient possible write for this operation.
        var now = DateTime.UtcNow;

        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(u => u.LastSeenAt, now),
                ct);
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    /// <summary>
    ///     Checks if the user holds a platform-level Admin or SuperAdmin role.
    ///     Uses the Identity UserRoles join table — single EXISTS query.
    ///     This bypasses course-level membership checks entirely.
    /// </summary>
    private async Task<bool> IsAdminOrSuperAdminAsync(
        string userId,
        CancellationToken ct = default)
    {
        return await db.UserRoles
            .AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == userId &&
                db.Roles
                    .Where(r => r.Name == DefaultRoles.Admin || r.Name == DefaultRoles.SuperAdmin)
                    .Select(r => r.Id)
                    .Contains(ur.RoleId),
                ct);
    }

    /// <summary>
    ///     Throws <see cref="UnauthorizedAccessException"/> if the user is NOT:
    ///     - A platform Admin/SuperAdmin, OR
    ///     - A course member with CourseRole.Level >= 3 (CoInstructor+).
    ///
    ///     Used by all channel management operations (create, update, delete, reorder).
    /// </summary>
    private async Task EnsureChannelManagementPermissionAsync(
        string requestingUserId,
        int courseId,
        CancellationToken ct = default)
    {
        // Platform Admin/SuperAdmin bypass
        if (await IsAdminOrSuperAdminAsync(requestingUserId, ct))
            return;

        // Course-level role check: Level >= 3 = CoInstructor or CourseOwner
        var hasPermission = await db.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == requestingUserId &&
                cu.CourseRole.Level >= 3 &&
                !cu.IsDeleted,
                ct);

        if (!hasPermission)
            throw new UnauthorizedAccessException(
                $"User {requestingUserId} does not have permission to manage channels in course {courseId}.");
    }

    // =========================================================================
    // Private — Shared Projection
    // =========================================================================

    /// <summary>
    ///     Reusable EF Core LINQ projection: Message entity → MessageDto.
    ///
    ///     Defined as a static field (not a method) so EF Core's expression
    ///     tree compiler sees a single stable <see cref="Expression"/> reference.
    ///     This avoids re-parsing the lambda on every call and allows EF Core
    ///     to cache the generated SQL query plan across requests.
    ///
    ///     Any change to MessageDto shape requires editing exactly this one place.
    /// </summary>
    private static readonly Expression<Func<Message, MessageDto>> MessageProjection = m =>
    new MessageDto(
        m.Id,
        m.ChannelId,
        m.SenderId,
        m.Sender.FirstName + " " + m.Sender.LastName,
        m.Sender.ImageUrl,
        m.Content,
        m.SentAt,
        m.EditedAt,
        m.IsDeleted,
        m.ReplyToMessageId,
        m.ReplyToMessage == null
            ? null
            : new ReplyPreviewDto(
                m.ReplyToMessage.Id,
                m.ReplyToMessage.Sender.FirstName + " " + m.ReplyToMessage.Sender.LastName,
                m.ReplyToMessage.Content.Length > 100
                    ? m.ReplyToMessage.Content.Substring(0, 100)
                    : m.ReplyToMessage.Content));
}
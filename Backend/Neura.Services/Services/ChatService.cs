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
        // Resolved in a single EXISTS query — no extra round trips
        var isSender = message.SenderId == requestingUserId;

        var isAdmin = !isSender && await db.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu =>
                cu.UserId == requestingUserId &&
                cu.CourseId == db.Channels
                    .Where(c => c.Id == message.ChannelId)
                    .Select(c => c.CourseId)
                    .FirstOrDefault() &&
                cu.CourseRole.Level >= 3 &&
                !cu.IsDeleted,
                ct);

        if (!isSender && !isAdmin)
            throw new UnauthorizedAccessException(
                "Only the sender or a course admin can delete this message.");

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
        var isMember = await db.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == requestingUserId &&
                !cu.IsDeleted,
                ct);

        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User {requestingUserId} is not a member of course {courseId}.");

        // Uses IX_Channels_CourseId_Position composite index.
        // HasQueryFilter automatically excludes IsDeleted channels.
        return await db.Channels
            .AsNoTracking()
            .Where(c => c.CourseId == courseId)
            .OrderBy(c => c.Position)
            .Select(c => new ChannelDto(
                c.Id,
                c.Name,
                c.Topic,
                c.Type,
                c.Position))
            .ToListAsync(ct)
            .ContinueWith(
                t => (IReadOnlyList<ChannelDto>)t.Result.AsReadOnly(),
                ct,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
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
    public async Task<IReadOnlyList<CourseMemberDto>> GetCourseMembersAsync(
        int courseId,
        string requestingUserId,
        CancellationToken ct = default)
    {
        // ── Security ──────────────────────────────────────────────────────────
        var isMember = await db.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == requestingUserId &&
                !cu.IsDeleted,
                ct);

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
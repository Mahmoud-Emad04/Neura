# 🔬 Neura Community & Chat Module — Senior Architectural Review

> **Reviewed:** `CommunityHub`, `InMemoryPresenceTracker`, `RedisPresenceTracker`, `ChatService`, `CommunityController`, all entities, DTOs, EF configurations, and `ApplicationDbContext`.

---

## 🚨 CRITICAL FLAWS (Fix Immediately)

### CRIT-1: `JoinChannel` — Membership Check Is Commented Out (Authorization Bypass)

**Severity: 🔴 Security**

**File:** [CommunityHub.cs:148-153](file:///c:/Users/josal/source/repos/Neura/Backend/Neura.Services/Hubs/CommunityHub.cs#L148-L153)

```csharp
// Lines 148-153 — COMMENTED OUT:
//var isMember = await chatService.IsCourseMemberAsync(userId, channelId);
//if (!isMember)
//{
//    await Clients.Caller.Error("You are not a member of this course.");
//    return;
//}
```

**Any authenticated user** can call `JoinChannel(channelId)` with any channel ID and start receiving full `MessageDto` payloads from courses they don't belong to. This also means `SendMessage` works for non-members, since `SaveMessageAsync` has the same check commented out (line 36-39).

This is a **complete bypass** of your hybrid strategy's security layer.

**Fix — Uncomment and enforce both gates:**

```csharp
// CommunityHub.cs — JoinChannel:
public async Task JoinChannel(int channelId)
{
    var userId = GetUserId();

    var isMember = await chatService.IsCourseMemberAsync(userId, channelId);
    if (!isMember)
    {
        await Clients.Caller.Error("You are not a member of this course.");
        return;
    }

    // ... rest of method
}
```

```csharp
// ChatService.cs — SaveMessageAsync:
var isMember = await IsCourseMemberAsync(senderId, channelId, ct);
if (!isMember)
    throw new UnauthorizedAccessException(
        $"User {senderId} is not a member of the course owning channel {channelId}.");
```

The same pattern is commented out in `GetChannelsAsync` (lines 201-211) and `GetCourseMembersAsync` (lines 285-287). **Uncomment all of them.**

> [!CAUTION]
> Until fixed, **any logged-in user can read and write messages in any course's channels**.

---

### CRIT-2: `OnConnectedAsync` — No Course Membership Validation

**Severity: 🔴 Security**

The `courseId` is read from the query string (`?courseId=5`) and **blindly trusted**. There is no check that the user is actually a member of that course. A malicious client can:

1. Connect with `?courseId=999`
2. Join the `course-999` group
3. Receive all `PresenceChanged` and `UnreadNotification` events for a course they aren't in
4. See who's online in a course they don't belong to (via `InitialPresenceSync`)

**Fix — Validate membership on connect:**

```csharp
public override async Task OnConnectedAsync()
{
    var userId = GetUserId();
    var courseId = GetCourseId();

    // ← ADD THIS: reject the connection if not a member
    var isMember = await chatService.IsCourseMemberByIdAsync(userId, courseId);
    if (!isMember)
    {
        Context.Abort();  // Terminates the connection immediately
        return;
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Course(courseId));
    // ... rest unchanged
}
```

You'll need a new method on `IChatService` that takes `courseId` directly (the existing one takes `channelId`):

```csharp
// IChatService:
Task<bool> IsCourseMemberByIdAsync(string userId, int courseId, CancellationToken ct = default);

// ChatService implementation:
public async Task<bool> IsCourseMemberByIdAsync(
    string userId, int courseId, CancellationToken ct = default)
{
    return await db.CourseUsers
        .AsNoTracking()
        .AnyAsync(cu =>
            cu.CourseId == courseId &&
            cu.UserId == userId &&
            !cu.IsDeleted,
            ct);
}
```

---

### CRIT-3: `PersistLastSeenAtSafeAsync` — Fire-and-Forget Captures Scoped DbContext

**Severity: 🔴 Runtime Crash / Data Corruption**

**File:** [CommunityHub.cs:120](file:///c:/Users/josal/source/repos/Neura/Backend/Neura.Services/Hubs/CommunityHub.cs#L120)

```csharp
_ = PersistLastSeenAtSafeAsync(result.UserId);  // fire-and-forget
```

`chatService` is injected into the Hub, which resolves it from the **scoped DI container**. When `OnDisconnectedAsync` returns, the scope is disposed. The fire-and-forget task may still be running, using a **disposed DbContext**. This will throw `ObjectDisposedException` intermittently — especially under load.

**Fix — Use `IServiceScopeFactory` to create an independent scope:**

```csharp
// Inject IServiceScopeFactory into the Hub:
[Authorize]
public sealed class CommunityHub(
    IPresenceTracker presenceTracker,
    IChatService chatService,
    IServiceScopeFactory scopeFactory)    // ← ADD
    : Hub<ICommunityHubClient>
{
    private async Task PersistLastSeenAtSafeAsync(string userId)
    {
        try
        {
            // Create an independent scope so the DbContext lives
            // as long as this background task — NOT the Hub scope.
            using var scope = scopeFactory.CreateScope();
            var scopedChatService = scope.ServiceProvider
                .GetRequiredService<IChatService>();

            await scopedChatService.PersistLastSeenAtAsync(userId);
        }
        catch (Exception ex)
        {
            // Log properly — Console.Error is not structured logging
            Console.Error.WriteLine(
                $"[CommunityHub] Failed to persist LastSeenAt for user {userId}: {ex.Message}");
        }
    }
}
```

---

### CRIT-4: `ApplicationDbContext.SaveChangesAsync` Crashes in SignalR Context

**Severity: 🔴 Runtime Crash**

**File:** [ApplicationDbContext.cs:76](file:///c:/Users/josal/source/repos/Neura/Backend/Neura.Repository/Persistence/ApplicationDbContext.cs#L76)

```csharp
var userId = _httpContextAccessor.HttpContext?
    .User.FindFirstValue(ClaimTypes.NameIdentifier)!;
```

SignalR Hub invocations **do not have an HttpContext** for WebSocket transport. `_httpContextAccessor.HttpContext` is `null` after the initial handshake. The null-forgiving `!` operator means `userId` is null, and then:

```csharp
entityEntry.Entity.CreatedById = userId;  // ← sets CreatedById = null
```

If `CreatedById` is a required column in SQL, this will throw a DB constraint violation on every `SaveChangesAsync` call from within the Hub.

**Fix — Fall back to the SignalR `Context.User` or make the field nullable-safe:**

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entries = ChangeTracker
        .Entries<AuditableEntity>()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

    // Gracefully handle missing HttpContext (SignalR WebSocket calls)
    var userId = _httpContextAccessor.HttpContext?
        .User.FindFirstValue(ClaimTypes.NameIdentifier);

    foreach (var entityEntry in entries)
    {
        if (entityEntry.State == EntityState.Added)
        {
            entityEntry.Entity.CreatedOn = DateTime.UtcNow;
            if (userId is not null)
                entityEntry.Entity.CreatedById = userId;
        }
        else
        {
            entityEntry.Entity.UpdatedOn = DateTime.UtcNow;
            if (userId is not null)
                entityEntry.Entity.UpdatedById = userId;
        }
    }

    return base.SaveChangesAsync(cancellationToken);
}
```

> [!IMPORTANT]
> A more robust solution is to pass the userId explicitly via `Message.Create(channelId, senderId, ...)` and set `CreatedById` in the entity's factory method, rather than relying on ambient context.

---

### CRIT-5: Missing `Message` EF Configuration — No Index for Cursor Pagination

**Severity: 🔴 Performance — Full Table Scan**

There is **no `MessageConfiguration` class** anywhere in the repository. The code comments reference `IX_Messages_ChannelId_Id_Desc`, but this index **does not exist**. Without it, `GetMessageHistoryAsync` performs a full table scan on every scroll.

There's also no global query filter for soft-deleted messages, and no configuration for the `ReplyToMessage` self-referencing FK.

**Fix — Create the configuration file:**

```csharp
// Neura.Repository/Persistence/EntitiesConfiguration/MessageConfiguration.cs

using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        // ── The cursor pagination index ────────────────────────────
        // Covers: WHERE ChannelId = @id AND Id < @cursor ORDER BY Id DESC
        // INCLUDE Sender join columns for the projection
        builder.HasIndex(m => new { m.ChannelId, m.Id })
               .IsDescending(false, true)
               .HasDatabaseName("IX_Messages_ChannelId_Id_Desc");

        // ── Sender lookup index ────────────────────────────────────
        builder.HasIndex(m => m.SenderId);

        // ── Content ────────────────────────────────────────────────
        builder.Property(m => m.Content)
               .IsRequired()
               .HasMaxLength(4_000);

        // ── Soft delete filter ─────────────────────────────────────
        builder.HasQueryFilter(m => !m.IsDeleted);

        // ── Relationships ──────────────────────────────────────────
        builder.HasOne(m => m.Channel)
               .WithMany(c => c.Messages)
               .HasForeignKey(m => m.ChannelId);

        builder.HasOne(m => m.Sender)
               .WithMany()
               .HasForeignKey(m => m.SenderId);

        // Self-referencing reply chain — SET NULL on parent delete
        builder.HasOne(m => m.ReplyToMessage)
               .WithMany()
               .HasForeignKey(m => m.ReplyToMessageId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

> [!WARNING]
> Without this, your cursor-based pagination query hits a **clustered index scan** on every channel scroll. For a chat table that grows to millions of rows, this is catastrophic.

---

## ⚡ CONCURRENCY & THREAD SAFETY

### Q2a: Are there race conditions in `InMemoryPresenceTracker`?

**✅ No race conditions in the in-memory implementation.** You correctly use a `SemaphoreSlim` to serialize all mutations. The `ConcurrentDictionary` alone wouldn't be sufficient because your operations span multiple dictionary lookups/writes (compound operations), but the semaphore makes them atomic. This is correct.

### Q2b: Does `OnDisconnectedAsync` handle the Multi-Tab Problem correctly?

**✅ Yes.** The logic is sound:

1. `UserConnectedAsync` returns `true` only when `connections.Count` goes from 0 → 1
2. `UserDisconnectedAsync` returns `UserWentOffline = true` only when `connections.Count` hits 0
3. The `HashSet<string>` tracks all connection IDs per user+course combo

**⚠️ One subtle issue with `GetOnlineUsersAsync`:**

```csharp
// Line 130 — reads kvp.Value.Count outside the lock:
.Where(kvp => kvp.Key.EndsWith(suffix) && kvp.Value.Count > 0)
```

`HashSet<string>` is **not thread-safe for concurrent reads and writes**. While the `SemaphoreSlim` protects mutations, `GetOnlineUsersAsync` deliberately skips the lock ("Read-only — no mutation, no lock needed"). But a concurrent `UserConnectedAsync` could be mutating the `HashSet` via `connections.Add()` while this `Count` check is iterating. This is a **data race**.

**Fix — Replace `HashSet<string>` with `ConcurrentBag<string>` or take the lock on reads too:**

```csharp
// Option A (simplest): Take the lock on reads too
public async Task<IReadOnlyList<string>> GetOnlineUsersAsync(int courseId)
{
    await _lock.WaitAsync();
    try
    {
        var suffix = $":{courseId}";
        return _presenceMap
            .Where(kvp => kvp.Key.EndsWith(suffix, StringComparison.Ordinal)
                       && kvp.Value.Count > 0)
            .Select(kvp => kvp.Key[..kvp.Key.LastIndexOf(':')])
            .ToList()
            .AsReadOnly();
    }
    finally
    {
        _lock.Release();
    }
}

// Same fix for IsOnlineAsync
```

---

## 📡 SIGNALR PAYLOAD & BANDWIDTH ANSWERS

### Q1a: Is the Hybrid Strategy properly isolated?

**✅ Yes — perfectly isolated.** The `SendMessage` method sends:

- `MessageDto` → only to `channel-{id}` group (line 234-236)
- `UnreadNotificationDto` → only to `course-{id}` group (line 250-256)

No heavy payload ever goes to the course group. The architecture is clean.

### Q1b: `UnreadNotification` broadcast sends to channel viewers too

**⚠️ Medium issue.** Line 250-251:

```csharp
await Clients
    .GroupExcept(HubGroups.Course(courseId), Context.ConnectionId)
    .UnreadNotification(...);
```

This excludes only the **sender's** connection. All other users in `course-{id}` receive the unread badge — **including users actively viewing that same channel**. They'll get both the `ReceiveMessage` AND the `UnreadNotification` for the same message.

Your comment says "Users actively in the channel group will ignore the UnreadNotification client-side because their channel is already active/focused." This is a reasonable approach, but it pushes filtering to the client.

**If you want server-side filtering**, you'd need to collect all connections currently in the channel group from the presence tracker:

```csharp
// Server-side approach (optional optimization):
var channelViewerConnectionIds = await presenceTracker
    .GetConnectionsInChannelAsync(request.ChannelId);

// Union with sender's connection
var excludeIds = channelViewerConnectionIds
    .Append(Context.ConnectionId)
    .ToList();

await Clients
    .GroupExcept(HubGroups.Course(courseId), excludeIds)
    .UnreadNotification(notification);
```

You'd need a new method on `IPresenceTracker`:

```csharp
Task<IReadOnlyList<string>> GetConnectionsInChannelAsync(int channelId);
```

This is a **nice-to-have** — client-side filtering works fine for moderate scale.

---

## 🗃️ DATABASE PERFORMANCE ANSWERS

### Q3: Is cursor-based pagination correct?

**✅ The LINQ is correct** — assuming the index exists. The query pattern is:

```csharp
.Where(m => m.ChannelId == channelId)
.Where(m => m.Id < beforeMessageId)    // cursor
.OrderByDescending(m => m.Id)
.Take(pageSize + 1)
.Select(MessageProjection)
```

This translates to:

```sql
SELECT TOP 51 ...
FROM Messages
WHERE ChannelId = @channelId AND Id < @cursor AND IsDeleted = 0
ORDER BY Id DESC
```

The `pageSize + 1` sentinel pattern is textbook. The `NextCursor` extraction from `rawMessages[^1].Id` is correct.

**❌ But the index doesn't exist** (CRIT-5). Without `IX_Messages_ChannelId_Id_Desc`, SQL Server will do a clustered index scan, not a seek.

### Additional EF Performance Issue: `GetChannelsAsync` Uses `.ContinueWith()`

**File:** [ChatService.cs:226-230](file:///c:/Users/josal/source/repos/Neura/Backend/Neura.Services/Services/ChatService.cs#L226-L230)

```csharp
return await db.Channels
    .ToListAsync(ct)
    .ContinueWith(
        t => (IReadOnlyList<ChannelDto>)t.Result.AsReadOnly(),
        ct, TaskContinuationOptions.OnlyOnRanToCompletion,
        TaskScheduler.Default);
```

`ContinueWith` is a legacy TPL pattern. It doesn't propagate exceptions cleanly and runs on `TaskScheduler.Default` (thread pool), losing the async context. Use `await` directly:

```csharp
var channels = await db.Channels
    .AsNoTracking()
    .Where(c => c.CourseId == courseId)
    .OrderBy(c => c.Position)
    .Select(c => new ChannelDto(c.Id, c.Name, c.Topic, c.Type, c.Position))
    .ToListAsync(ct);

return channels.AsReadOnly();
```

### `DeleteMessageAsync` — Subquery Anti-Pattern

```csharp
// ChatService.cs lines 164-174:
var isAdmin = !isSender && await db.CourseUsers
    .AnyAsync(cu =>
        cu.UserId == requestingUserId &&
        cu.CourseId == db.Channels                    // ← subquery inside AnyAsync
            .Where(c => c.Id == message.ChannelId)
            .Select(c => c.CourseId)
            .FirstOrDefault() &&                      // ← comparing int to bool?
        cu.CourseRole.Level >= 3 &&
        !cu.IsDeleted, ct);
```

This has a **type error**: `cu.CourseId == db.Channels...FirstOrDefault()` is comparing `int` to an `int` subquery result, but the `&&` boolean chain makes this read like `cu.CourseId == (bool)`. EF Core *might* translate this, but it's fragile. Also, `FirstOrDefault()` returns `0` if no channel is found, which could match a real `CourseId`.

**Fix — Split into two clear queries:**

```csharp
var channelCourseId = await db.Channels
    .AsNoTracking()
    .Where(c => c.Id == message.ChannelId)
    .Select(c => c.CourseId)
    .FirstAsync(ct);

var isAdmin = !isSender && await db.CourseUsers
    .AsNoTracking()
    .AnyAsync(cu =>
        cu.UserId == requestingUserId &&
        cu.CourseId == channelCourseId &&
        cu.CourseRole.Level >= 3 &&
        !cu.IsDeleted,
        ct);
```

---

## 🛡️ RESILIENCE & EDGE CASES

### Q4a: Rate Limiting (50 messages in 2 seconds)

**❌ There is zero rate limiting.** A malicious client can call `SendMessage` in a tight loop and:

1. Flood the SQL database with INSERT operations
2. Broadcast to all channel viewers at maximum speed
3. Cause `UnreadNotification` spam to the entire course group

**Fix — Add a Hub-level rate limiter:**

```csharp
// Option A: Simple in-memory per-user throttle
[Authorize]
public sealed class CommunityHub(
    IPresenceTracker presenceTracker,
    IChatService chatService,
    IServiceScopeFactory scopeFactory)
    : Hub<ICommunityHubClient>
{
    // Static: shared across all Hub instances for the same user
    private static readonly ConcurrentDictionary<string, DateTime> _lastMessageTime = new();
    private static readonly TimeSpan MinMessageInterval = TimeSpan.FromMilliseconds(500);

    public async Task SendMessage(SendMessageHubRequest request)
    {
        var userId = GetUserId();

        // ── Rate limit check ──
        var now = DateTime.UtcNow;
        if (_lastMessageTime.TryGetValue(userId, out var lastTime)
            && (now - lastTime) < MinMessageInterval)
        {
            await Clients.Caller.Error("You are sending messages too quickly.");
            return;
        }
        _lastMessageTime[userId] = now;

        // ... rest of SendMessage
    }
}
```

For production, use ASP.NET Core's built-in rate limiting middleware or a sliding window counter in Redis.

### Q4b: DbContext Scoping in SignalR Hubs

**✅ Correctly handled — with one exception.** SignalR Hubs are transient, but ASP.NET Core creates a **new DI scope per Hub invocation** (not per connection). This means:

- `chatService` gets a fresh `DbContext` per method call ✅
- The `DbContext` is disposed at the end of each invocation ✅

**❌ The exception is the fire-and-forget in `OnDisconnectedAsync`** (CRIT-3), which outlives the scope.

### Q4c: `SendMessage` Doesn't Validate the `SendMessageHubRequest` 

The `[Required]`, `[MinLength]`, `[MaxLength]` attributes on `SendMessageHubRequest` are **not automatically validated** by SignalR. Unlike ASP.NET Core MVC controllers, **SignalR does not run model validation**. A client can send an empty string or a 1MB content payload.

**Fix — Validate manually in the Hub method:**

```csharp
public async Task SendMessage(SendMessageHubRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 4_000)
    {
        await Clients.Caller.Error("Message content must be between 1 and 4000 characters.");
        return;
    }

    if (request.ChannelId <= 0)
    {
        await Clients.Caller.Error("Invalid channel ID.");
        return;
    }

    // ... rest of method
}
```

---

## 🔄 REDIS MIGRATION READINESS

### Redis Tracker: Race Condition in `UserConnectedAsync`

**File:** [RedisPresenceTracker.cs:63-68](file:///c:/Users/josal/source/repos/Neura/Backend/Neura.Services/Helpers/RedisPresenceTracker.cs#L63-L68)

```csharp
await transaction.ExecuteAsync();

// ← Gap: another connection could SADD between ExecuteAsync and SetLengthAsync
var count = await _db.SetLengthAsync(presenceKey);
return count == 1;
```

The `SCARD` call happens **outside** the transaction. Between `EXEC` and `SCARD`, another connection from the same user could `SADD`, making `count == 2` even though this was the first real connection. This means two tabs opening simultaneously could both return `justCameOnline = false`, and no `PresenceChanged` broadcast fires.

**Fix — Use a Lua script for atomicity:**

```csharp
public async Task<bool> UserConnectedAsync(
    string userId, int courseId, string connectionId)
{
    var connKey = ConnKeyPrefix + connectionId;
    var presenceKey = PresenceKey(userId, courseId);

    // Lua script: atomic set metadata + SADD + return previous SCARD
    const string script = """
        redis.call('HSET', KEYS[1], 'userId', ARGV[1], 'courseId', ARGV[2], 'channelId', '')
        redis.call('EXPIRE', KEYS[1], ARGV[4])
        local prevCount = redis.call('SCARD', KEYS[2])
        redis.call('SADD', KEYS[2], ARGV[3])
        redis.call('EXPIRE', KEYS[2], ARGV[5])
        return prevCount
        """;

    var prevCount = (long)await _db.ScriptEvaluateAsync(
        script,
        keys: [connKey, presenceKey],
        values: [userId, courseId.ToString(), connectionId,
                 ((int)ConnTtl.TotalSeconds).ToString(),
                 ((int)PresenceTtl.TotalSeconds).ToString()]);

    return prevCount == 0;  // Was empty before our SADD = first connection
}
```

### Redis Tracker: `GetOnlineUsersAsync` Uses `KEYS` Command

```csharp
var server = redis.GetServer(redis.GetEndPoints().First());
var pattern = $"{PresenceKeyPrefix}*:{courseId}";
await foreach (var key in server.KeysAsync(pattern: pattern))
```

`KEYS` (even as `SCAN` via `KeysAsync`) is **O(N) over all keys in Redis** and **blocks the Redis server** during execution. In production with thousands of users, this will cause latency spikes.

**Fix:** Maintain a separate `course:{courseId}:online` SET that you `SADD`/`SREM` atomically alongside the presence set. Then `GetOnlineUsersAsync` becomes a single `SMEMBERS` call.

---

## 📋 SENIOR REFACTORING SUGGESTIONS

### REF-1: Content Sanitization — No XSS Protection on Messages

`ChatService.SaveMessageAsync` stores `content` directly. Unlike your Exam module (which uses `HtmlSanitizer`), chat messages have **zero HTML sanitization**. If the React frontend renders `Content` with `dangerouslySetInnerHTML`, this is a stored XSS vulnerability.

**Fix:** Either sanitize server-side via `HtmlSanitizer` or ensure the React client always renders as plain text.

### REF-2: `SendMessage` Extra DB Query for `GetCourseIdForChannelAsync`

After `SaveMessageAsync` already looked up the channel (and validated it), `SendMessage` makes **another query** to get the courseId:

```csharp
var courseId = await chatService.GetCourseIdForChannelAsync(request.ChannelId);
```

**Fix:** Return the `courseId` alongside the `MessageDto` from `SaveMessageAsync`, or include `CourseId` in `MessageDto`.

### REF-3: Logging Uses `Console.Error.WriteLine`

```csharp
Console.Error.WriteLine(
    $"[CommunityHub] Failed to persist LastSeenAt for user {userId}: {ex.Message}");
```

This bypasses your structured logging pipeline entirely. Inject `ILogger<CommunityHub>` and use it:

```csharp
_logger.LogError(ex, "Failed to persist LastSeenAt for user {UserId}", userId);
```

---

## 📊 Summary Matrix

| Area | Grade | Key Finding |
|------|-------|-------------|
| **Hybrid Group Isolation** | ✅ A | `MessageDto` → channel only; `UnreadNotification` → course only |
| **Channel Join Security** | ❌ F | Membership check commented out — any user joins any channel |
| **Connect Security** | ❌ F | No course membership validation on `OnConnectedAsync` |
| **Presence Multi-Tab** | ✅ A- | Logic correct; minor `HashSet` thread-safety on reads |
| **Cursor Pagination LINQ** | ✅ A | Textbook pattern; `pageSize+1` sentinel correct |
| **Pagination Index** | ❌ F | `MessageConfiguration` doesn't exist — no index in DB |
| **DbContext Scoping** | 🟠 C | Correct per-invocation scope, but fire-and-forget escapes it |
| **SaveChanges in SignalR** | ❌ D | `HttpContextAccessor` is null for WebSocket transport |
| **Rate Limiting** | ❌ F | Zero throttling — message spam floods DB and SignalR |
| **Input Validation** | ❌ D | SignalR doesn't auto-validate `DataAnnotations` |
| **Redis Migration** | 🟠 B- | Good structure; race condition in `UserConnectedAsync`; `KEYS` command |
| **DDD Encapsulation** | ✅ A | `Channel` and `Message` have private setters + factory methods |

---

> **Priority order for fixes:**
> 1. **CRIT-1 + CRIT-2** — Uncomment auth checks; add connect-time membership validation
> 2. **CRIT-5** — Create `MessageConfiguration` with the pagination index
> 3. **CRIT-4** — Fix `SaveChangesAsync` for SignalR WebSocket context
> 4. **CRIT-3** — Fix fire-and-forget DbContext scope escape
> 5. Add rate limiting to `SendMessage`
> 6. Add manual input validation in Hub methods
> 7. Fix `GetOnlineUsersAsync` `HashSet` thread safety

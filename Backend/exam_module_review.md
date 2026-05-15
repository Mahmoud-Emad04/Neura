# 🔬 Neura Exam Module — Senior Architectural Review

> **Reviewed:** All entities, DTOs, services, controllers, EF configurations, and authorization handlers across Core, Services, Repository, and API layers.

---

## 🚨 CRITICAL ARCHITECTURAL FLAWS (Fix Immediately)

### CRIT-1: `GetResultsAsync` Leaks Correct Answers Even When `ShowCorrectAnswersAfterSubmit = false`

**Severity: 🔴 Security — Data Leak**

The `Exam` entity has `ShowCorrectAnswersAfterSubmit`, but **`GetResultsAsync` in `ExamAttemptService` completely ignores it**. Every submitted attempt returns `IsCorrect` on every `OptionResultResponse` — regardless of the instructor's setting.

A student who fails attempt 1 gets a full answer key, then aces attempt 2.

**File:** [ExamAttemptService.cs](file:///c:/Users/josal/source/repos/Neura/Backend/Neura.Services/Services/ExamAttemptService.cs#L396-L515)

**Fix — Gate the answer reveal:**

```csharp
// In GetResultsAsync, after line 401, load the Exam and check the flag:
var showAnswers = attempt.Exam.ShowCorrectAnswersAfterSubmit;

// Then in the option mapping (around line 470-478), conditionally hide:
var optionResults = question.AnswerOptions
    .OrderBy(a => a.Order)
    .Select(a => new OptionResultResponse
    {
        OptionId = a.Id,
        Text = a.Text,
        IsCorrect = showAnswers ? a.IsCorrect : false,     // ← GATED
        WasSelected = selectedOptionIds.Contains(a.Id)
    }).ToList();

// Also gate the per-question IsCorrect/EarnedPoints:
questionResults.Add(new QuestionResultResponse
{
    QuestionId = question.Id,
    QuestionText = question.QuestionText,
    QuestionType = question.QuestionType,
    Points = question.Points,
    EarnedPoints = showAnswers ? earnedPoints : 0,          // ← GATED
    IsCorrect = showAnswers ? isCorrect : false,            // ← GATED
    IsAnswered = isAnswered,
    Options = showAnswers ? optionResults : new()            // ← GATED
});
```

---

### CRIT-2: `ExamService.GetByIdAsync` / `GetByLessonIdAsync` — No Authorization Check (Commented Out)

**Severity: 🔴 Security — Broken Access Control**

Both instructor-only endpoints have their permission checks **commented out**:

```csharp
// Lines 89-90 in ExamService.cs:
//if (!await HasInstructorPermissionAsync(courseId, userId))
//    return Result.Failure<ExamDetailResponse>(ExamErrors.Forbidden);
```

This means **any authenticated user** (including students) can call `GET /api/exams/{lessonId}` and receive the full `ExamDetailResponse`, which contains `QuestionResponse` objects with `AnswerOptionResponse.IsCorrect = true`. This is a **complete answer key leak**.

The same pattern exists in `DeleteAsync` and `UnpublishAsync`.

**Fix — Either re-enable the checks or rely on the `[HasExamPermission]` attribute consistently:**

```csharp
// Option A: Re-enable inline checks
var courseId = exam.Lesson.Section.CourseId;
if (!await HasInstructorPermissionAsync(courseId, userId))
    return Result.Failure<ExamDetailResponse>(ExamErrors.Forbidden);

// Option B: Add [HasExamPermission] on the controller actions (GetById, GetByLessonId)
// In ExamsController.cs:
[HttpGet("{lessonId:int}", Name = nameof(GetById))]
[HasExamPermission(Core.Enums.CoursePermission.EditContent)]  // ← ADD THIS
public async Task<IActionResult> GetById([FromRoute] int lessonId) { ... }
```

> [!CAUTION]
> Until this is fixed, any enrolled student can extract the full answer key by calling the instructor's exam detail endpoint.

---

### CRIT-3: `PublishAsync` Logic Is Inverted — Dead Code Branch

**Severity: 🔴 Logic Bug — Publish Never Works Correctly**

```csharp
// ExamService.cs lines 189-230:
public async Task<Result> PublishAsync(int lessonId, string userId)
{
    // ...
    if (exam.IsPublished)     // ← Already published? Run validation & publish again?
    {
        // validation logic...
        exam.IsPublished = true;  // no-op, already true
    }
    else
    {
        if (!exam.IsPublished)    // ← Always true in this branch
            return Result.Failure(ExamErrors.AlreadyUnpublished); // ← ALWAYS returns error
        // ... unreachable code below
    }
}
```

The `else` branch is entered when `IsPublished == false` (the actual publish case), but it immediately checks `!exam.IsPublished` which is always `true`, so it **always returns `AlreadyUnpublished`**. Publishing an unpublished exam is impossible.

**Fix:**

```csharp
public async Task<Result> PublishAsync(int lessonId, string userId)
{
    var exam = await _context.Exams
        .Include(e => e.Lesson).ThenInclude(l => l.Section)
        .Include(e => e.Questions).ThenInclude(q => q.AnswerOptions)
        .FirstOrDefaultAsync(e => e.LessonId == lessonId);

    if (exam is null)
        return Result.Failure(ExamErrors.ExamNotFound);

    if (exam.IsPublished)
        return Result.Failure(ExamErrors.AlreadyPublished);

    // Validation for publishing
    if (!exam.Questions.Any())
        return Result.Failure(ExamErrors.NoQuestions);

    var hasInvalidQuestions = exam.Questions
        .Any(q => !q.AnswerOptions.Any(a => a.IsCorrect));
    if (hasInvalidQuestions)
        return Result.Failure(ExamErrors.QuestionsWithoutCorrectAnswer);

    if (exam.NumberOfQuestionsToServe.HasValue
        && exam.NumberOfQuestionsToServe.Value > exam.Questions.Count)
        return Result.Failure(ExamErrors.PoolSizeExceedsTotalQuestions);

    exam.IsPublished = true;
    exam.UpdatedById = userId;
    exam.UpdatedOn = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return Result.Success();
}
```

---

### CRIT-4: Race Condition — `StartAttemptAsync` Has No Concurrency Guard

**Severity: 🔴 Integrity — Student Can Exceed MaxAttempts**

The check for `existingInProgress` and `attemptsTaken` is **not atomic**. Two rapid requests from the same student can both pass the `AnyAsync` check before either inserts, creating duplicate in-progress attempts or exceeding `MaxAttempts`.

**Fix — Add a unique filtered index + catch the DB exception:**

```csharp
// In ExamAttemptConfiguration.cs, add:
builder.HasIndex(a => new { a.ExamId, a.UserId })
       .HasFilter("[Status] = 'InProgress'")
       .IsUnique();
```

```csharp
// In StartAttemptAsync, wrap the insert:
try
{
    _context.ExamAttempts.Add(attempt);
    await _context.SaveChangesAsync();
}
catch (DbUpdateException) // Unique index violation
{
    return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.AttemptAlreadyInProgress);
}
```

---

### CRIT-5: `ViolationRequest.OccurredAt` — Client Controls the Timestamp

**Severity: 🟠 Security — Anti-Cheat Bypass**

The `RecordViolationAsync` method trusts the client-supplied `request.OccurredAt`. A malicious student can send a future timestamp or manipulate the violation timeline.

**Fix — Always use server time:**

```csharp
var violation = new AttemptViolation
{
    ExamAttemptId = attemptId,
    ViolationType = request.ViolationType,
    OccurredAt = DateTime.UtcNow  // ← Server-authoritative, ignore client
};
```

---

### CRIT-6: Soft-Deleted Questions Still Served to Students

**Severity: 🟠 Data Integrity**

`Question.IsDeleted` exists but is **never filtered** in `StartAttemptAsync`, `GetExamInfoAsync`, `GradeAttemptAsync`, or the analytics queries. A soft-deleted question can still be served in new attempts.

**Fix — Add global query filter or explicit `.Where(!q.IsDeleted)`:**

```csharp
// Option A: Global filter in QuestionConfiguration.cs:
builder.HasQueryFilter(q => !q.IsDeleted);

// Option B: Explicit filter in StartAttemptAsync (line 145):
var allQuestions = exam.Questions.Where(q => !q.IsDeleted).ToList();
```

---

## ⚡ PERFORMANCE ISSUES

### PERF-1: N+1 Query in `GetStudentAttemptsAsync` (Analytics)

**Severity: 🔴 Critical at Scale**

Lines 196-205 of `ExamAnalyticsService.cs`:

```csharp
foreach (var attempt in attempts)   // ← N iterations
{
    var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder);
    var points = await _context.Questions   // ← 1 query per attempt = N+1
        .Where(q => questionOrder.Contains(q.Id))
        .SumAsync(q => q.Points);
}
```

With 20 attempts per page, this fires **20 extra queries**.

**Fix — Batch it into a single query:**

```csharp
// Collect all question IDs across all attempts, then query once:
var allQuestionIds = attempts
    .SelectMany(a => JsonSerializer.Deserialize<List<int>>(a.QuestionOrder) ?? new())
    .Distinct()
    .ToList();

var questionPointsLookup = await _context.Questions
    .AsNoTracking()
    .Where(q => allQuestionIds.Contains(q.Id))
    .ToDictionaryAsync(q => q.Id, q => q.Points);

var attemptTotalPoints = new Dictionary<int, decimal>();
foreach (var attempt in attempts)
{
    var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();
    attemptTotalPoints[attempt.Id] = questionOrder
        .Where(id => questionPointsLookup.ContainsKey(id))
        .Sum(id => questionPointsLookup[id]);
}
```

---

### PERF-2: `GetExamAnalyticsAsync` Loads ALL Attempts Into Memory

**Severity: 🟠 Will degrade at scale**

Line 47-50:

```csharp
var allAttempts = await _context.ExamAttempts
    .Where(a => a.ExamId == exam.Id)
    .ToListAsync();  // ← Pulls EVERY attempt row into memory
```

For a course with 5,000 students × 3 attempts each = 15,000 rows materialized.

**Fix — Push aggregation to SQL:**

```csharp
var stats = await _context.ExamAttempts
    .AsNoTracking()
    .Where(a => a.ExamId == exam.Id)
    .GroupBy(a => 1)
    .Select(g => new
    {
        TotalAttempts = g.Count(),
        UniqueStudents = g.Select(a => a.UserId).Distinct().Count(),
        InProgressCount = g.Count(a => a.Status == AttemptStatus.InProgress),
        CompletedCount = g.Count(a => a.Status != AttemptStatus.InProgress),
        AvgScore = g.Where(a => a.Score != null).Average(a => (decimal?)a.Score),
        AvgPercentage = g.Where(a => a.ScorePercentage != null)
                         .Average(a => (decimal?)a.ScorePercentage),
        Highest = g.Max(a => (decimal?)a.ScorePercentage),
        Lowest = g.Where(a => a.ScorePercentage != null)
                  .Min(a => (decimal?)a.ScorePercentage),
        PassedCount = g.Count(a => a.Passed == true),
        FailedCount = g.Count(a => a.Passed == false),
        TimedOutCount = g.Count(a => a.Status == AttemptStatus.TimedOut),
        AutoSubmittedCount = g.Count(a => a.Status == AttemptStatus.AutoSubmitted),
    })
    .FirstOrDefaultAsync();
```

---

### PERF-3: `questionOrder.Contains(q.Id)` Translates Poorly to SQL

**Severity: 🟠 Performance**

This pattern appears in `GradingService`, `GetResultsAsync`, `GetAttemptTotalPointsAsync`, and analytics. EF translates `List<int>.Contains()` to `WHERE Id IN (...)`, which is fine for ≤50 items, but the list is deserialized from JSON at runtime, defeating query plan caching.

**Consider:** Adding a join table `ExamAttemptQuestion` instead of JSON for `QuestionOrder`. This makes SQL JOINs possible and eliminates all runtime deserialization.

---

### PERF-4: Missing Index — `AttemptAnswers` by `QuestionId` Alone

The `AttemptAnswerConfiguration` has an index on `{ExamAttemptId, QuestionId}`, which is correct for attempt-scoped lookups. However, `BuildExamDetailResponseAsync` queries:

```csharp
.Where(aa => questionIds.Contains(aa.QuestionId))  // No AttemptId filter
```

This needs an index on `QuestionId` alone:

```csharp
// In AttemptAnswerConfiguration.cs:
builder.HasIndex(a => a.QuestionId);
```

---

## 🔒 SECURITY & ANTI-CHEATING ANSWERS

### Q1: Does the API leak correct answers during an active attempt?

**During the attempt: ✅ No (well-designed).** `AttemptOptionResponse` correctly omits `IsCorrect`. The `StartAttemptResponse` and `BuildStartAttemptResponse` never expose correctness.

**After the attempt: ❌ Yes — leaks unconditionally.** `GetResultsAsync` ignores `ShowCorrectAnswersAfterSubmit` (see CRIT-1). Also, the instructor endpoints (`GetByIdAsync`, `GetByLessonIdAsync`) expose `IsCorrect` to any authenticated user because auth is commented out (CRIT-2).

### Q2: Is grading securely server-side?

**✅ Yes.** `GradingService.GradeAttemptAsync` fetches correct answers from the DB, computes the score server-side, and the client has no influence on the score calculation. This is solid.

**⚠️ However**, the `SaveAnswerRequest` only validates that option IDs belong to the question — it doesn't validate that the options belong to the questions **actually served** to this student (from the attempt's `QuestionOrder`). A student could theoretically save answers to questions not in their randomized subset, though the grading only scores served questions so the impact is limited.

---

## 🗃️ STATE & PROGRESS PERSISTENCE ANSWERS

### Q3: Is progress saved if the internet drops?

**✅ Architecturally sound.** The `SaveAnswerAsync` endpoint persists each answer individually to the DB the moment the student selects it. The `ResumeAttemptAsync` correctly rehydrates the attempt with `SavedOptionIds`. The `ExamTimeoutJob` handles background auto-grading for timed-out attempts.

**⚠️ One gap:** There's no **client-side offline queue** mechanism. If the network drops mid-save, the answer is lost silently. Consider:

1. Adding a `LastSavedAt` timestamp to the response so the frontend can detect stale saves.
2. Returning a `SaveAnswerResponse` with the saved state instead of bare `Ok()`.

```csharp
// Suggested SaveAnswerResponse:
public class SaveAnswerResponse
{
    public int QuestionId { get; set; }
    public List<int> SavedOptionIds { get; set; } = new();
    public DateTime SavedAt { get; set; }
}
```

---

## 🏗️ DDD PURITY ANSWERS

### Q4a: Entity Encapsulation — Grade: D

**Every entity is a pure anemic data bag.** All properties are `{ get; set; }` with no invariant protection:

| Issue | Example |
|-------|---------|
| No private setters | `Exam.PassingScorePercentage` can be set to -500 or 9999 from anywhere |
| No factory methods | `ExamAttempt` is constructed with `new ExamAttempt { ... }` scattered across services |
| No domain methods | Grading logic lives in `GradingService`, not on the entity |
| Navigation collections are mutable `List<T>` | Should be `IReadOnlyCollection<T>` backed by `private List<T>` |

**Recommended encapsulation for `ExamAttempt`:**

```csharp
public class ExamAttempt
{
    public int Id { get; private set; }
    public int ExamId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime StartedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public decimal? Score { get; private set; }
    public decimal? ScorePercentage { get; private set; }
    public bool? Passed { get; private set; }
    public AttemptStatus Status { get; private set; }

    private ExamAttempt() { } // EF constructor

    public static ExamAttempt Start(int examId, string userId,
        string questionOrderJson, string answerOrderJson)
    {
        return new ExamAttempt
        {
            ExamId = examId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            Status = AttemptStatus.InProgress,
            QuestionOrder = questionOrderJson,
            AnswerOrder = answerOrderJson
        };
    }

    public void Grade(decimal score, decimal totalPossible,
        decimal passingPercentage, AttemptStatus status)
    {
        if (Status != AttemptStatus.InProgress)
            throw new InvalidOperationException("Cannot grade a non-active attempt.");

        Score = score;
        ScorePercentage = totalPossible > 0
            ? Math.Round((score / totalPossible) * 100, 2) : 0;
        Passed = ScorePercentage >= passingPercentage;
        Status = status;
        SubmittedAt = DateTime.UtcNow;
    }
}
```

### Q4b: Relationship Modeling — Grade: B+

The relational model is **well-structured** for SQL Server:

- ✅ `Exam → Question → AnswerOption` — clean 1:N chains
- ✅ `ExamAttempt → AttemptAnswer → AttemptAnswerOption` — proper many-to-many through join entity
- ✅ Composite indexes on `{ExamId, UserId}` and filtered index on `{Status, StartedAt}`
- ✅ Unique index on `{ExamAttemptId, QuestionId}` for AttemptAnswer

**One concern:** Storing `QuestionOrder` and `AnswerOrder` as JSON strings in `nvarchar(max)` prevents SQL-side JOINs. For a high-traffic system, a proper `ExamAttemptQuestion` join table would be more performant and queryable.

---

## 📋 SENIOR REFACTORING SUGGESTIONS (Best Practices)

### REF-1: Massive Code Duplication — Results Building Logic

The result-building logic (iterate questions, check correctness, build `QuestionResultResponse`) is **copy-pasted identically** in:

1. `ExamAttemptService.GetResultsAsync` (lines 396-515)
2. `ExamAnalyticsService.GetStudentAttemptDetailAsync` (lines 243-363)

Extract to a shared domain service:

```csharp
public class AttemptResultBuilder
{
    public static AttemptResultResponse Build(
        ExamAttempt attempt,
        List<Question> questions,
        List<int> questionOrder,
        bool showCorrectAnswers)
    {
        // Single source of truth for result computation
    }
}
```

---

### REF-2: `ExamAttemptService` Directly Uses `ApplicationDbContext` — Violates Clean Architecture

Every service method directly queries `_context` with inline LINQ. This couples your Application/Domain layer to EF Core.

**Minimum fix:** Extract the most repeated query patterns into repository methods:

```csharp
public interface IExamRepository
{
    Task<Exam?> GetWithQuestionsAndOptionsAsync(int lessonId);
    Task<ExamAttempt?> GetAttemptWithAnswersAsync(int attemptId);
    Task<bool> HasInProgressAttemptAsync(int examId, string userId);
    Task<int> GetAttemptCountAsync(int examId, string userId);
}
```

---

### REF-3: `HasInstructorPermissionAsync` Duplicated 4 Times

The exact same method is duplicated in:
- `ExamAnalyticsService` (line 540)
- `QuestionService` (line 301)
- `ExamService` (commented out, line 300)
- `ExamPermissionAuthorizationHandler` (different approach)

Consolidate into your existing `ICoursePermissionService`.

---

### REF-4: `ExamInfoResponse.ExamId` Is Set to `lessonId` — Wrong Value

```csharp
// ExamAttemptService.cs line 84:
ExamId = lessonId,  // ← Should be exam.Id
```

This is a data bug in the response DTO. Frontend receives a `lessonId` disguised as `ExamId`.

---

### REF-5: `StartAttemptController` Returns Two Responses

```csharp
// ExamAttemptController.cs lines 51-56:
if (result.IsSuccess)
    return StatusCode(StatusCodes.Status201Created, result.Value);

return result.IsSuccess    // ← Dead code: already handled above
    ? Ok(result.Value)
    : result.ToProblem();
```

Fix: Remove the dead branch.

---

### REF-6: `ExamPermissionAuthorizationHandler` Uses Synchronous EF Query

```csharp
// Line 39 — uses FirstOrDefault() not FirstOrDefaultAsync()
var courseId = _context.Exams
    .Where(e => e.LessonId == examId)
    .Select(e => (int?)e.Lesson.Section.CourseId)
    .FirstOrDefault();  // ← Synchronous! Blocks the thread pool.
```

Fix: Use `await ... .FirstOrDefaultAsync()`.

---

## 📊 Summary Matrix

| Area | Grade | Key Finding |
|------|-------|-------------|
| **Answer Leak (During Attempt)** | ✅ A | `AttemptOptionResponse` omits `IsCorrect` |
| **Answer Leak (Post-Submit)** | ❌ F | `ShowCorrectAnswersAfterSubmit` ignored |
| **Answer Leak (Instructor Endpoints)** | ❌ F | Auth commented out — any user sees answer key |
| **Grading Security** | ✅ A | Fully server-side |
| **N+1 Queries** | ❌ D | `GetStudentAttemptsAsync` fires N queries; analytics loads all rows |
| **Indexes** | ✅ B+ | Good filtered index; missing `QuestionId` standalone index |
| **Progress Persistence** | ✅ B+ | Auto-save works; resume works; no save confirmation to client |
| **DDD Encapsulation** | ❌ D | Pure anemic model, all public setters |
| **Code Duplication** | ⚠️ C | Results builder copy-pasted; auth helper duplicated 4× |
| **Publish/Unpublish Logic** | ❌ F | Inverted if/else makes publish impossible |

---

> **Priority order for fixes:**
> 1. CRIT-3 (Publish logic — broken feature)
> 2. CRIT-2 (Commented-out auth — full answer leak)
> 3. CRIT-1 (`ShowCorrectAnswersAfterSubmit` ignored)
> 4. CRIT-4 (Race condition on attempt start)
> 5. CRIT-6 (Soft-deleted questions still served)
> 6. PERF-1 (N+1 in analytics)
> 7. Everything else

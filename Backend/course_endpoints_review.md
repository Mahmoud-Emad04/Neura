# Course Endpoints Review

**Project:** Neura Backend
**Date:** 2026-05-12
**Reviewer:** Claude Code

---

## Table of Contents

1. [CoursesController](#1-coursescontroller)
2. [EnrollmentController](#2-enrollmentcontroller)
3. [SectionsController](#3-sectionscontroller)
4. [LessonsController](#4-lessonscontroller)
5. [CourseProgressController](#5-courseprogresscontroller)
6. [CourseTeamController](#6-coursteamcontroller)
7. [ExamsController](#7-examscontroller)
8. [Summary of Issues](#8-summary-of-issues)
9. [Priority Improvements](#9-priority-improvements)

---

## 1. CoursesController

### `GET /api/courses` — GetAll

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses` |
| **Auth** | AllowAnonymous |
| **Service** | `CourseService.GetAllAsync` |

**Time Complexity:** O(N + M) where N = paginated courses, M = total lessons queried per course. **N+1 query problem.**

**Issues:**
- After the initial paginated query, a separate `ToListAsync` query runs **per course** to fetch lesson durations (`_context.Lessons.Where(...)`) — this is an N+1 anti-pattern. For a page of 20 courses, that's 20+ extra queries.
- Bookmarks and enrollment status queries also run after pagination, meaning additional DB round trips based on page size.
- `Path.Combine` on `ImageUrl` is used in a post-materialization callback — correct but fragile.

**Verdict:** **Needs Fix** — The per-course lesson query should be replaced with a single grouped query before pagination, or the lesson count/duration should be stored as denormalized columns on the Course entity.

**Suggestions:**
- Add `TotalLessons` and `TotalDurationMinutes` columns to the `Course` entity and update them when lessons are added/removed.
- Combine the bookmarks + enrollment check into a single `ContainsAsync` call per page instead of two separate queries.

---

### `GET /api/courses/{courseId}/content` — GetContentById

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/{courseId}/content` |
| **Auth** | AllowAnonymous |
| **Service** | `CourseService.GetContentByIdAsync` |

**Time Complexity:** O(S + L) where S = sections, L = lessons. Acceptable.

**Issues:**
- Two separate queries are made: one for the course existence check and one for sections+lessons. These could be combined.
- When userId is provided, `LessonCompletions` are fetched with a separate query — acceptable for a single course.
- The `Select` on `Lessons` includes `l.Exam.Questions.Count()` — if not properly indexed or if lazy loading triggers, this could be slow for exams with many questions.

**Verdict:** **Acceptable** — Works well for a single course. No immediate fixes needed, but monitor for performance at scale.

**Suggestions:**
- Combine the existence check into the main sections query using `.FirstOrDefaultAsync` or check existence via `.AnyAsync()` only when needed.

---

### `GET /api/courses/{courseId}/metadata` — GetCourseMetadata

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/{courseId}/metadata` |
| **Auth** | AllowAnonymous |
| **Service** | `CourseService.GetCourseMetadataAsync` → `BuildCourseMetadataResponse` |

**Time Complexity:** O(1) — single course query + up to 3 additional queries (owner user, enrollment check, bookmark check). Acceptable.

**Issues:**
- Calls `BuildCourseMetadataResponse` which makes **3 additional sequential queries**: owner user lookup, CourseUser lookup, student count, bookmark check. These are all independent and could run in parallel with `Task.WhenAll`.

**Verdict:** **Minor Issue** — Works but not optimal.

**Suggestions:**
- Parallelize the independent queries using `Task.WhenAll`.

---

### `POST /api/courses` — Create

| Aspect | Details |
|---|---|
| **Route** | `POST /api/courses` |
| **Auth** | Authorize (no `[InstructorOnly]` attribute — commented out) |
| **Service** | `CourseService.CreateAsync` |

**Time Complexity:** O(1) — constant DB operations.

**Issues:**
- The `[InstructorOnly]` attribute is commented out on line 124 of the controller. Anyone authenticated can create a course.
- Tags are validated by count comparison — good.
- Image upload is optional and defaults correctly — good.
- `SaveChangesAsync` is called once — good transactional behavior.

**Verdict:** **Security Issue** — Commented-out `[InstructorOnly]` means no role restriction.

**Suggestions:**
- Uncomment or implement the `[InstructorOnly]` attribute to restrict course creation to instructors only.

---

### `PUT /api/courses/{courseId}` — UpdateDetails

| Aspect | Details |
|---|---|
| **Route** | `PUT /api/courses/{courseId}` |
| **Auth** | `[HasCoursePermission(CoursePermission.EditContent)]` |
| **Service** | `CourseService.UpdateAsync` |

**Time Complexity:** O(1) — single course update.

**Issues:**
- Removes and re-inserts `LearningOutcomes` and `Prerequisites` rather than doing an upsert — causes orphaned rows temporarily if the request fails mid-way.
- Tags are cleared and re-added the same way — same concern.
- Image deletion and re-upload are in the same transaction — correct.

**Verdict:** **Acceptable with caveats** — The current approach works but creates orphaned child rows momentarily. For high-traffic courses, consider upsert logic.

**Suggestions:**
- Use bulk upsert (e.g., `ExecuteUpdateAsync`) for child entities to avoid orphaned rows.

---

### `PUT /api/courses/{courseId}/cover-image` — UpdateImage

| Aspect | Details |
|---|---|
| **Route** | `PUT /api/courses/{courseId}/cover-image` |
| **Auth** | `[HasCoursePermission(CoursePermission.EditContent)]` |
| **Service** | `CourseService.UpdateImageAsync` |

**Time Complexity:** O(1) — single update + optional file delete.

**Issues:** None. Clean implementation.

**Verdict:** **Good**

---

### `GET /api/courses/my/editable` — GetEditableCourses

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/my/editable` |
| **Auth** | Authorize |
| **Service** | `CourseService.GetEditableCoursesAsync` |

**Time Complexity:** O(N + 2) where N = editable courses. **Multiple sequential queries that could be combined.**

**Issues:**
- `totalOwnedCourses` and `totalCoInstructorCourses` are fetched with two separate `CountAsync` queries — could be one query.
- The pagination is done manually: first query gets IDs, then a second query filters by those IDs. This is a "pagination via ID list" anti-pattern that doesn't scale.
- `studentCounts` is a separate query per page — acceptable.
- `orderedItems` uses a `Select` + `First` loop to reorder by original ID order — O(N^2) in the worst case.

**Verdict:** **Needs Fix** — The manual pagination and ID-list approach is fragile and inefficient.

**Suggestions:**
- Remove the two-step pagination (get IDs then filter). Use proper offset pagination directly in the query.
- Combine `totalOwnedCourses` and `totalCoInstructorCourses` into a single query using conditional counting.
- Use a dictionary lookup for `orderedItems` instead of the `First` loop.

---

### `POST /api/courses/{courseId}/bookmark` — ToggleBookmark

| Aspect | Details |
|---|---|
| **Route** | `POST /api/courses/{courseId}/bookmark` |
| **Auth** | Authorize (implicit via `User.GetUserId()`) |
| **Service** | `CourseService.ToggleBookmarkAsync` |

**Time Complexity:** O(1)

**Issues:** None. Clean idempotent toggle.

**Verdict:** **Good**

---

### `GET /api/courses/bookmarked` — GetBookmarked

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/bookmarked` |
| **Auth** | Authorize |
| **Service** | `CourseService.GetBookmarkedAsync` |

**Time Complexity:** O(N) where N = bookmarked courses. Acceptable.

**Issues:** None. Uses specification pattern correctly.

**Verdict:** **Good**

---

### `GET /api/courses/{courseId}/status` — GetStatus

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/{courseId}/status` |
| **Auth** | `[HasCoursePermission(CoursePermission.ViewAnalytics)]` |
| **Service** | `CourseService.GetCourseStatusAsync` |

**Time Complexity:** O(1) + O(1) if Pending (activates `GetActivationRequirementsAsync`)

**Issues:**
- `GetActivationRequirementsAsync` does a single query with `SelectMany` — efficient.
- The route uses `{courseId}` as a string (hashId) which is correct for consistency.

**Verdict:** **Good**

---

### `POST /api/courses/{courseId}/activate` — ActivateCourse

| Aspect | Details |
|---|---|
| **Route** | `POST /api/courses/{courseId}/activate` |
| **Auth** | `[HasCoursePermission(CoursePermission.ManageSettings)]` |
| **Service** | `CourseService.ActivateCourseAsync` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `POST /api/courses/{courseId}/complete` — CompleteCourse

| Aspect | Details |
|---|---|
| **Route** | `POST /api/courses/{courseId}/complete` |
| **Auth** | `[HasCoursePermission(CoursePermission.ManageSettings)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `POST /api/courses/{courseId}/reactivate` — ReactivateCourse

| Aspect | Details |
|---|---|
| **Route** | `POST /api/courses/{courseId}/reactivate` |
| **Auth** | `[HasCoursePermission(CoursePermission.ManageSettings)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `POST /api/courses/{courseId}/unpublish` — UnpublishCourse

| Aspect | Details |
|---|---|
| **Route** | `POST /api/courses/{courseId}/unpublish` |
| **Auth** | `[HasCoursePermission(CoursePermission.ManageSettings)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `DELETE /api/courses/{courseId}` — DeleteCourse

| Aspect | Details |
|---|---|
| **Route** | `DELETE /api/courses/{courseId}` |
| **Auth** | `[HasCoursePermission(CoursePermission.DeleteCourse)]` |
| **Service** | `CourseService.DeleteCourseAsync` |

**Time Complexity:** O(1)

**Issues:**
- The `courseUser` variable is fetched (line 792-799) but **never used** — dead code.
- `GetEditableCoursesAsync` also has a dead `courseUser` query on line 292.
- Only does a soft delete (`IsDeleted = true`) — no cascade to sections/lessons/enrollments. They remain orphaned in queries that filter by `!IsDeleted`.

**Verdict:** **Minor Issue** — Dead code should be removed. Soft-delete without cascade is a design decision but should be documented.

**Suggestions:**
- Remove the unused `courseUser` query.
- Consider whether sections/lessons should also be soft-deleted when a course is deleted.

---

## 2. EnrollmentController

### `POST /api/courses/{courseId}/enroll` — Enroll

| Aspect | Details |
|---|---|
| **Route** | `POST /api/courses/{courseId}/enroll` |
| **Auth** | Authorize |
| **Service** | `EnrollmentService.EnrollAsync` |

**Time Complexity:** O(1)

**Issues:**
- No check for `IsEnrollmentOpen` flag on the course — anyone can enroll even if the flag is false.
- Logs a warning for paid courses but still allows enrollment — correct placeholder behavior for now.
- Reactivation of soft-deleted enrollments is handled correctly.

**Verdict:** **Acceptable** — Consider adding `IsEnrollmentOpen` check before allowing enrollment.

**Suggestions:**
- Add `IsEnrollmentOpen` check from the course entity before enrollment.

---

### `POST /api/courses/{courseId:int}/unenroll` — Unenroll

| Aspect | Details |
|---|---|
| **Route** | `POST /api/courses/{courseId:int}/unenroll` |
| **Auth** | Authorize |

**Time Complexity:** O(1)

**Issues:** None. Good guard against unenrolling owners/team members.

**Verdict:** **Good**

---

### `GET /api/courses/{courseId}/enrollment-status` — GetEnrollmentStatus

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/{courseId}/enrollment-status` |
| **Auth** | AllowAnonymous |
| **Service** | `EnrollmentService.GetEnrollmentStatusAsync` |

**Time Complexity:** O(1)

**CRITICAL BUG — Line 132:**

```csharp
// WRONG:
if (TryDecodeCourseId(keyId, out var courseId))
    return Result.Failure<EnrollmentStatusResponse>(EnrollmentErrors.CourseNotFound);

// SHOULD BE:
if (!TryDecodeCourseId(keyId, out var courseId))
    return Result.Failure<EnrollmentStatusResponse>(EnrollmentErrors.CourseNotFound);
```

The condition is **inverted**. It returns "not found" when decoding **succeeds**, and continues when it **fails**.

**Verdict:** **Critical Bug** — Must be fixed immediately.

---

### `GET /api/courses/enrolled` — GetMyEnrolledCourses

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/enrolled` |
| **Auth** | Authorize |
| **Service** | `EnrollmentService.GetMyEnrolledCoursesAsync` |

**Time Complexity:** O(N) where N = enrolled courses. **N+1 issue.**

**Issues:**
- `TotalLessons` and `NumberOfLessons` both run `Count()` on the same subquery — duplicated work.
- `ProgressPercentage` and `CompletedLessons` are always set to `null` and `0` — stub values.
- `Hours` computed post-materialization requires a second query per page — acceptable but not ideal.
- Uses `.ThenInclude(c => c.CreatedBy)` which does a JOIN — good.
- Bookmarks are checked per course with `AnyAsync` — another N+1 risk.

**Verdict:** **Needs Fix** — The stub values for progress should be either removed or properly calculated.

**Suggestions:**
- Use a single query to get lesson counts and durations grouped by courseId to avoid per-course queries.
- Either implement progress calculation properly or remove the fields from the response DTO.

---

### `GET /api/courses/teaching` — GetMyTeachingCourses

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/teaching` |
| **Auth** | Authorize |

**Time Complexity:** O(N) with eager loading. **Potential N+1 on large result sets.**

**Issues:**
- Eager loads entire course hierarchy (`Sections` → `Lessons`) in memory before counting. For instructors with many courses, this loads a lot of unnecessary data.
- `GetMyInvitations` is called internally but not exposed as an endpoint — dead code path.

**Verdict:** **Needs Fix** — Use projection instead of eager loading for sections/lessons.

**Suggestions:**
- Replace eager loading with a projected query that only counts lessons instead of loading all lesson entities into memory.

---

### `GET /api/courses/{courseId:int}/students` — GetCourseStudents

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/{courseId:int}/students` |
| **Auth** | `[HasCoursePermission(CoursePermission.ViewAnalytics)]` |

**Time Complexity:** O(S + log P) where S = students, P = total. Good offset pagination.

**Issues:** None. Clean implementation.

**Verdict:** **Good**

---

### `POST /api/courses/{courseId:int}/students` — AddStudent

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ManageStudents)]` |

**Time Complexity:** O(1)

**Issues:** None. Handles reactivation of soft-deleted enrollments — good.

**Verdict:** **Good**

---

### `DELETE /api/courses/{courseId:int}/students/{studentId}` — RemoveStudent

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ManageStudents)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `GET /api/courses/enrollment-dashboard` — GetEnrollmentDashboard

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/enrollment-dashboard` |
| **Auth** | Authorize |
| **Service** | `EnrollmentService.GetEnrollmentDashboardAsync` |

**Time Complexity:** O(N) where N = enrolled courses. **5 separate queries.**

**Issues:**
- Makes 5 sequential queries when they could be combined into fewer using `Task.WhenAll`.
- `foreach` loop to compute completed/in-progress courses is in-memory — acceptable.

**Verdict:** **Acceptable** — Consider parallelizing the queries.

**Suggestions:**
- Use `Task.WhenAll` to run the independent queries (enrolled IDs, total lessons, completed lessons, durations) in parallel.

---

## 3. SectionsController

### `GET /api/courses/{courseId}/sections` — GetAllByCourse

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/{courseId}/sections` |
| **Auth** | AllowAnonymous |
| **Service** | `SectionService.GetAllByCourseAsync` |

**Time Complexity:** O(S) where S = sections.

**Issues:**
- Does not return lessons — correct for a lightweight endpoint.
- `Sections` endpoint has `AllowAnonymous` but the nested `GetById` has `[HasSectionPermission]` — inconsistent auth model.

**Verdict:** **Good**

---

### `POST /api/courses/{courseId}/sections` — Create

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None. Properly validates position conflict.

**Verdict:** **Good**

---

### `PUT /api/sections/{sectionId}` — Update

| Aspect | Details |
|---|---|
| **Auth** | `[HasSectionPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:**
- Uses `Adapt` on the request to update the entity — this is fragile if the DTO has fields that shouldn't be updated directly.
- Position conflict check is done before the update — correct.

**Verdict:** **Acceptable**

**Suggestions:**
- Explicitly map only the fields that should be updated instead of using `Adapt` blindly.

---

### `PUT /api/sections/{sectionId}/status` — ToggleStatus

| Aspect | Details |
|---|---|
| **Auth** | `[HasSectionPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None. Toggle is implemented correctly (soft-delete/recover).

**Verdict:** **Good**

---

### `DELETE /api/sections/{sectionId}` — Delete

| Aspect | Details |
|---|---|
| **Auth** | `[HasSectionPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

## 4. LessonsController

### `POST /api/sections/{sectionId}/init` — Initialize

| Aspect | Details |
|---|---|
| **Auth** | `[HasSectionPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:**
- `sectionId < 1` validation is redundant — EF Core would fail on invalid IDs anyway.
- The `positionConflict` check for duplicate positions is good.
- `HasEditPermission` check is commented out (lines 38-54) — **security concern**. Permission is handled by the attribute but the commented code suggests it was intended to be a fallback.

**Verdict:** **Acceptable** — Remove commented-out code or clarify its purpose.

---

### `PUT /api/lessons/{id}/position` — UpdatePosition

| Aspect | Details |
|---|---|
| **Auth** | `[HasLessonPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(M) where M = affected lessons in position range.

**Issues:** None. Efficient batch update of affected lessons.

**Verdict:** **Good**

---

### `PUT /api/lessons/{id}/privacy` — UpdatePrivacy

| Aspect | Details |
|---|---|
| **Auth** | `[HasLessonPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `PUT /api/lessons/{id}` — UpdateLesson

| Aspect | Details |
|---|---|
| **Auth** | `[HasLessonPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `GET /api/lessons/section/{sectionId}` — GetSectionLessons

| Aspect | Details |
|---|---|
| **Auth** | `[Authorize]` (but no permission attribute) |

**Time Complexity:** O(L) where L = lessons in section.

**Issues:**
- **Missing permission check.** Any authenticated user can view lessons in any section by knowing the section ID. Should have `[HasSectionPermission(CoursePermission.ViewContent)]`.
- Uses `Include` + in-memory filter instead of `Where` in the query — works but less efficient than filtering at the DB level.

**Verdict:** **Security Issue** — Add permission attribute.

---

### `PUT /api/lessons/{id}/article` — UpdateArticleContent

| Aspect | Details |
|---|---|
| **Auth** | `[HasLessonPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:**
- HTML sanitization is done server-side using `HtmlSanitizer` — good for XSS prevention.

**Verdict:** **Good**

---

### `GET /api/lessons/{id}/article` — GetArticleContent

| Aspect | Details |
|---|---|
| **Auth** | Authorize (no explicit permission) |

**Issues:**
- The access guard (lines 265-274) is **commented out** — meaning anyone can read any article lesson by ID regardless of enrollment. This is a **security issue**.

**Verdict:** **Security Issue** — Uncomment the access guard or add proper permission check.

---

### `GET /api/lessons/{id}/video/link` — GetVideoLink

| Aspect | Details |
|---|---|
| **Auth** | `[Authorize]` (no explicit permission attribute on controller) |

**Issues:**
- No `[HasLessonPermission]` attribute — relies on `IVideoService` to enforce access. Verify that `GetVideoLinkAsync` properly checks enrollment/ownership.

**Verdict:** **Acceptable** — Verify the service enforces access control.

---

### `POST /api/lessons/{id}/video/signed-upload` — GetSignedVideoUploadCredentials

| Aspect | Details |
|---|---|
| **Auth** | `[HasLessonPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `POST /api/lessons/{id}/video/finalize` — FinalizeVideoUpload

| Aspect | Details |
|---|---|
| **Auth** | `[HasLessonPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `DELETE /api/lessons/{id}` — DeleteLesson

| Aspect | Details |
|---|---|
| **Auth** | `[HasLessonPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:**
- If Cloudinary deletion fails, the lesson is still marked as deleted in the DB but the video URL remains — leads to orphaned video storage.
- For Quiz lessons, `ExecuteUpdateAsync` soft-deletes the Exam but doesn't handle ExamQuestions/ExamAttempts.

**Verdict:** **Minor Issue** — Add a transaction to ensure DB and Cloudinary deletion are atomic, or use a background job for Cloudinary cleanup.

---

### `POST /api/lessons/{lessonId}/complete` — MarkCompleted

| Aspect | Details |
|---|---|
| **Auth** | Authorize |

**Time Complexity:** O(1)

**Issues:**
- Checks `isEnrolled` with `AnyAsync` then calls `isEnrolled` again in the service — acceptable.
- The service also checks `isEnrolled` again and updates `LastAccessedOn` — double-check is acceptable for safety.

**Verdict:** **Good** — Idempotent implementation.

---

## 5. CourseProgressController

### `GET /api/courses/{keyId}/progress` — GetProgress

| Aspect | Details |
|---|---|
| **Route** | `GET /api/courses/{keyId}/progress` |
| **Auth** | Authorize |
| **Service** | `LessonProgressService.GetCourseProgressAsync` |

**Time Complexity:** O(L) where L = lessons in course. **2 sequential queries.**

**Issues:**
- Two sequential queries: `GetOrderedAccessibleLessonsAsync` then `LessonCompletions` query. These could be combined into one query using a left join.

**Verdict:** **Acceptable** — Works but can be optimized.

**Suggestions:**
- Combine into a single query with left join to get lessons and completion status in one DB round trip.

---

### `GET /api/courses/{keyId}/progress/next-lesson` — GetNextLesson

| Aspect | Details |
|---|---|
| **Auth** | Authorize |

**Time Complexity:** O(L) — **Calls `GetCourseProgressAsync` internally, which is wasteful.**

**Issues:**
- `GetNextLessonAsync` calls `GetCourseProgressAsync` and then only uses the `NextLesson` field, discarding all other data. This means **the full progress calculation runs twice** for a simple "next lesson" lookup.
- For users who call this endpoint frequently (e.g., on every lesson completion), this is wasteful.

**Verdict:** **Performance Issue** — Implement a dedicated lightweight query for next lesson.

**Suggestions:**
- Create a dedicated `GetNextLessonIdAsync` method that only finds the first incomplete lesson without computing all progress stats.

---

## 6. CourseTeamController

### `GET /api/courses/{courseId:int}/team` — GetTeamOverview

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ViewAnalytics)]` |

**Time Complexity:** O(M + I) where M = members, I = invitations. Two queries.

**Issues:** None. Good implementation.

**Verdict:** **Good**

---

### `GET /api/courses/{courseId:int}/team/members` — GetTeamMembers

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ViewContent)]` |

**Time Complexity:** O(M)

**Issues:** None.

**Verdict:** **Good**

---

### `GET /api/courses/{courseId:int}/team/members/{userId}` — GetTeamMember

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ViewAnalytics)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `POST /api/courses/{courseId:int}/team/invite` — InviteTeamMember

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ManageTeam)]` |

**Time Complexity:** O(1)

**Issues:**
- `InviteTeamMemberAsync` makes **many sequential queries**: course check, requester check, team count, existing user, existing membership, existing invitation, role — 7 queries. These could be reduced to 3-4 with better structuring.
- `UserManager.FindByEmailAsync` is called separately from the DB query — could cause two user lookups.
- Email sending is a `TODO` — if this is production, emails are missing.

**Verdict:** **Performance Issue** — Too many sequential queries. Email sending needs implementation.

**Suggestions:**
- Combine the permission check and course existence into one query.
- Consider a single "pre-flight" query to validate all preconditions at once.

---

### `DELETE /api/courses/{courseId:int}/team/members/{userId}` — RemoveTeamMember

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ManageTeam)]` |

**Time Complexity:** O(1)

**Issues:** None. Good guard checks.

**Verdict:** **Good**

---

### `PATCH /api/courses/{courseId:int}/team/members/{userId}/role` — ChangeTeamRole

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ManageTeam)]` |

**Time Complexity:** O(1) — **But 3 sequential DB queries.**

**Issues:**
- Fetches requester, new role, then target member — could be combined into 1-2 queries.

**Verdict:** **Minor Issue** — Could be optimized.

---

### `POST /api/courses/{courseId:int}/team/transfer` — TransferOwnership

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.TransferOwnership)]` |

**Time Complexity:** O(1) — **4 sequential queries.**

**Issues:** None. Proper transactional integrity with role switching.

**Verdict:** **Good**

---

### `GET /api/courses/{courseId:int}/team/invitations` — GetPendingInvitations

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ManageTeam)]` |

**Time Complexity:** O(I)

**Issues:** None.

**Verdict:** **Good**

---

### `DELETE /api/courses/{courseId:int}/team/invitations/{invitationId:int}` — CancelInvitation

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ManageTeam)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `POST /api/courses/{courseId:int}/team/invitations/{invitationId:int}/resend` — ResendInvitation

| Aspect | Details |
|---|---|
| **Auth** | `[HasCoursePermission(CoursePermission.ManageTeam)]` |

**Time Complexity:** O(1)

**Issues:** None. Email sending is a TODO.

**Verdict:** **Good** (pending email implementation)

---

## 7. ExamsController

### `POST /api/exams` — Create

| Aspect | Details |
|---|---|
| **Auth** | Authorize |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `GET /api/exams/{lessonId:int}` — GetById

| Aspect | Details |
|---|---|
| **Auth** | Authorize |

**Time Complexity:** O(Q) where Q = questions.

**Issues:**
- Same functionality as `GetByLessonId`. Having both is redundant.

**Verdict:** **Minor Issue** — Consider removing `GetByLessonId` since `GetById` already uses lessonId.

---

### `GET /api/exams/by-lesson/{lessonId:int}` — GetByLessonId

| Aspect | Details |
|---|---|
| **Auth** | Authorize |

**Issues:** Redundant — same as `GetById`.

**Verdict:** **Remove** — Redundant endpoint.

---

### `PUT /api/exams/{lessonId:int}/settings` — UpdateSettings

| Aspect | Details |
|---|---|
| **Auth** | `[HasExamPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `PUT /api/exams/{lessonId:int}/publish` — Publish

| Aspect | Details |
|---|---|
| **Auth** | `[HasExamPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `PUT /api/exams/{lessonId:int}/unpublish` — Unpublish

| Aspect | Details |
|---|---|
| **Auth** | `[HasExamPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None.

**Verdict:** **Good**

---

### `DELETE /api/exams/{lessonId:int}` — Delete

| Aspect | Details |
|---|---|
| **Auth** | `[HasExamPermission(CoursePermission.EditContent)]` |

**Time Complexity:** O(1)

**Issues:** None. Properly cascades to questions.

**Verdict:** **Good**

---

## 8. Summary of Issues

### Critical Bugs
| # | Endpoint | Issue |
|---|---|---|
| 1 | `GetEnrollmentStatus` (EnrollmentController:132) | **Inverted condition** — returns "not found" on success, continues on failure |
| 2 | `Create` (CoursesController:124) | `[InstructorOnly]` attribute **commented out** — anyone authenticated can create courses |

### Security Issues
| # | Endpoint | Issue |
|---|---|---|
| 3 | `GetArticleContent` (LessonsController) | Access guard **commented out** — anyone can read any article |
| 4 | `GetSectionLessons` (LessonsController) | **Missing permission attribute** — any authenticated user can view any section's lessons |
| 5 | `Create` (CoursesController) | No instructor role restriction enforced |

### N+1 / Performance Issues
| # | Endpoint | Issue |
|---|---|---|
| 6 | `GetAll` (CoursesController) | **Per-course lesson query** — N extra queries for N paginated courses |
| 7 | `GetEditableCourses` (CoursesController) | Manual pagination with ID list, O(N²) reordering |
| 8 | `GetMyTeachingCourses` (EnrollmentController) | Eager loads entire course hierarchy into memory |
| 9 | `GetNextLesson` (CourseProgressController) | Calls full `GetCourseProgressAsync` — wastes computation |
| 10 | `InviteTeamMember` (CourseTeamController) | 7 sequential queries — could be 3-4 |

### Code Quality Issues
| # | Location | Issue |
|---|---|---|
| 11 | `DeleteCourse`, `GetEditableCourses` | **Dead code** — `courseUser` variable fetched but never used |
| 12 | `GetByLessonId` | **Redundant endpoint** — same as `GetById` |
| 13 | `Initialize` (LessonsController) | `HasEditPermission` check commented out with `TODO` — should be removed or implemented |
| 14 | `Create` | No email sent on course creation (logs only) |
| 15 | `InviteTeamMember`, `ResendInvitation` | Email sending is `TODO` — invitations have no email delivery |

---

## 9. Priority Improvements

### P0 — Fix Immediately
1. **Fix the inverted `TryDecodeCourseId` condition** in `GetEnrollmentStatusAsync` (EnrollmentController:132)
2. **Uncomment or re-implement the `[InstructorOnly]`** attribute on course creation
3. **Uncomment the access guard** in `GetArticleContentAsync` or implement proper permission check

### P1 — High Priority
4. **Fix N+1 in `GetAll`** — Replace per-course lesson queries with a single grouped query or denormalize
5. **Add missing permission** to `GetSectionLessons` — `[HasSectionPermission(CoursePermission.ViewContent)]`
6. **Fix `GetNextLesson`** — Create a lightweight dedicated query instead of calling full progress
7. **Optimize `GetEditableCourses`** — Remove manual pagination anti-pattern

### P2 — Medium Priority
8. **Parallelize queries** in `BuildCourseMetadataResponse` and `GetEnrollmentDashboardAsync` using `Task.WhenAll`
9. **Remove dead code** — unused `courseUser` queries in `DeleteCourse` and `GetEditableCourses`
10. **Remove redundant endpoint** — `GetByLessonId` duplicates `GetById`
11. **Add `IsEnrollmentOpen` check** in `EnrollAsync`

### P3 — Nice to Have
12. **Implement email sending** for course invitations
13. **Upsert pattern** for `LearningOutcomes` and `Prerequisites` in `UpdateAsync` to avoid orphaned rows
14. **Denormalize** `TotalLessons` and `TotalDuration` on the Course entity for faster `GetAll` queries
15. **Add `TotalLessons` and `CompletedLessons`** calculation in `GetMyEnrolledCourses` or remove stub fields

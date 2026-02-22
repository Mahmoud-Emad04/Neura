# 📚 Neura API Documentation

> REST API reference for the Neura Learning Management System — .NET 10

---

## Table of Contents

- [🔐 Authentication](#-authentication)
- [👤 Account (Me)](#-account-me)
- [📖 Courses](#-courses)
- [📑 Sections](#-sections)
- [🎬 Lessons](#-lessons)
- [⭐ Reviews](#-reviews)
- [📢 Announcements](#-announcements)
- [👥 Users](#-users)

---

## 🔐 Authentication

### `POST` /auth/login — `Public`

Authenticates a user with username/email and password. Returns a JWT access token and a refresh token.

**Request Body**

```json
{
  "userNameOrEmail": "john@example.com",
  "password": "P@ssw0rd!"
}
```

| Status | Description |
|--------|-------------|
| `200`  | Returns `AuthResponse` with token and refresh token. |
| `400`  | Invalid credentials or account not confirmed. |

---

### `POST` /auth/register — `Public`

Creates a new user account and sends an email confirmation code.

**Request Body**

```json
{
  "userName": "johndoe",
  "email": "john@example.com",
  "discordHandle": "john#1234",
  "firstName": "John",
  "lastName": "Doe",
  "password": "P@ssw0rd!"
}
```

| Status | Description |
|--------|-------------|
| `200`  | Registration successful. Confirmation email sent. |
| `400`  | Validation error (e.g., duplicate email, weak password). |

---

### `POST` /auth/refresh — `Public`

Refreshes an expired JWT token using a valid refresh token.

**Request Body**

```json
{
  "token": "expired-jwt-token",
  "refreshToken": "refresh-token-value"
}
```

| Status | Description |
|--------|-------------|
| `200`  | New tokens issued. |
| `400`  | Invalid or expired refresh token. |

---

### `POST` /auth/revoke — `Public`

Revokes a refresh token (logout).

**Request Body**

```json
{
  "token": "jwt-token",
  "refreshToken": "refresh-token-to-revoke"
}
```

| Status | Description |
|--------|-------------|
| `200`  | Token revoked successfully. |
| `400`  | Invalid token. |

---

### `POST` /auth/confirm-email — `Public`

Confirms a user's email address with the code sent during registration.

**Request Body**

```json
{
  "userId": "user-id",
  "code": "confirmation-code"
}
```

| Status | Description |
|--------|-------------|
| `200`  | Email confirmed. Auth tokens returned. |
| `400`  | Invalid code or user. |

---

### `POST` /auth/resend-confirmation — `Public`

Resends the email confirmation code.

**Request Body**

```json
{
  "email": "john@example.com"
}
```

| Status | Description |
|--------|-------------|
| `200`  | Confirmation email resent. |
| `400`  | Email not found or already confirmed. |

---

### `POST` /auth/forgot-password — `Public`

Sends a password reset code to the user's email.

**Request Body**

```json
{
  "email": "john@example.com"
}
```

| Status | Description |
|--------|-------------|
| `200`  | Reset code sent to email. |
| `400`  | Email not found. |

---

### `POST` /auth/reset-password — `Public`

Resets the password using the code from the forgot-password email.

**Request Body**

```json
{
  "email": "john@example.com",
  "code": "reset-code",
  "newPassword": "NewP@ssw0rd!"
}
```

| Status | Description |
|--------|-------------|
| `200`  | Password reset successful. |
| `400`  | Invalid code or weak password. |

---

### `GET` /auth/external-login/{provider} — `Public`

Redirects the user to the external OAuth provider. After authentication the user is redirected back to `/auth/external-callback` which issues tokens and redirects to the frontend.

| Parameter  | Type   | Description            |
|------------|--------|------------------------|
| `provider` | string | `Google` or `GitHub`   |

---

## 👤 Account (Me)

### `GET` /me — 🔒 Auth

Returns the current authenticated user's profile.

| Status | Description |
|--------|-------------|
| `200`  | Returns the user profile. |

---

### `PUT` /me — 🔒 Auth

Updates the current user's profile details.

**Request Body**

```json
{
  "firstName": "John",
  "lastName": "Doe"
}
```

| Status | Description |
|--------|-------------|
| `204`  | Profile updated. |

---

### `PUT` /me/change-password — 🔒 Auth

Changes the current user's password.

**Request Body**

```json
{
  "currentPassword": "OldP@ss1",
  "newPassword": "NewP@ss1"
}
```

| Status | Description |
|--------|-------------|
| `204`  | Password changed. |
| `400`  | Current password is incorrect or new password is too weak. |

---

## 📖 Courses

### `GET` /api/courses — `Public`

Returns a paginated list of course summaries. Anonymous access is allowed; authenticated users also get bookmark and enrollment status.

**Query Parameters**

| Name            | Type    | Default | Description              |
|-----------------|---------|---------|--------------------------|
| `pageNumber`    | int     | 1       | Page number (≥ 1)        |
| `pageSize`      | int     | 10      | Items per page (1–50)    |
| `searchValue`   | string? | null    | Search by title          |
| `sortColumn`    | string? | null    | Column to sort by        |
| `sortDirection` | string? | ASC     | `ASC` or `DESC`          |
| `isFree`        | bool?   | null    | Filter free/paid courses |

| Status | Description |
|--------|-------------|
| `200`  | Returns `PaginatedList<CourseSummaryResponse>` |

---

### `GET` /api/courses/{courseId}/content — `Public`

Returns the course's section and lesson hierarchy. Use the public HashId (e.g., `Xy7zK`), not the integer database ID.

| Parameter  | Type   | Description     |
|------------|--------|-----------------|
| `courseId`  | string | Hashed course ID |

| Status | Description |
|--------|-------------|
| `200`  | Returns `CourseResponse` (KeyId, Hours, Sections[]) |
| `404`  | Course not found or invalid HashId. |

---

### `GET` /api/courses/{courseId}/metadata — `Public`

Returns detailed course information: description, tags, learning outcomes, prerequisites, enrollment count, rating, and the caller's enrollment/bookmark/owner status.

| Parameter  | Type   | Description     |
|------------|--------|-----------------|
| `courseId`  | string | Hashed course ID |

**Response Body (200)**

```json
{
  "keyId": "Xy7zKp3mN2q",
  "title": "Introduction to AI",
  "description": "...",
  "imageUrl": "https://...",
  "startin": "2025-09-01",
  "endin": "2025-12-01",
  "price": 0,
  "rating": 4.5,
  "totalReviews": 23,
  "numberOfStudents": 150,
  "isEnrolled": true,
  "isBookmarked": false,
  "isOwner": false,
  "tags": [{ "id": 1, "name": "AI" }],
  "learningOutcomes": ["Understand neural networks"],
  "prerequisites": ["Basic Python"]
}
```

| Status | Description |
|--------|-------------|
| `200`  | Returns `CourseMetadataResponse` |
| `404`  | Course not found. |

---

### `GET` /api/courses/my-learning — 🔒 Auth

Returns courses where the current user is enrolled as a student (excludes owned courses and soft-deleted enrollments).

| Status | Description |
|--------|-------------|
| `200`  | Returns `IEnumerable<CourseMetadataResponse>` |

---

### `GET` /api/courses/bookmarked — 🔒 Auth

Returns a paginated list of courses the current user has bookmarked.

**Query Parameters**

| Name            | Type    | Default | Description    |
|-----------------|---------|---------|----------------|
| `pageNumber`    | int     | 1       | Page number    |
| `pageSize`      | int     | 10      | Items per page |
| `searchValue`   | string? | null    | Search filter  |
| `sortColumn`    | string? | null    | Sort column    |
| `sortDirection` | string? | ASC     | `ASC` / `DESC` |

| Status | Description |
|--------|-------------|
| `200`  | Returns `PaginatedList<CourseSummaryResponse>` |

---

### `POST` /api/courses — 🔒 Auth

Creates a course and assigns the authenticated user as owner. Returns a `Location` header pointing to the new course's content endpoint.

**Request Body**

```json
{
  "title": "Introduction to AI",
  "description": "Learn AI fundamentals...",
  "price": 0,
  "startin": "2025-09-01",
  "endin": "2025-12-01",
  "tags": [1, 3],
  "learningOutcomes": ["Understand neural networks"],
  "prerequisites": ["Basic Python"]
}
```

| Status | Description |
|--------|-------------|
| `201`  | Course created. `Location` header included. |
| `400`  | Validation failed (invalid tags, dates, etc.). |

---

### `PUT` /api/courses/{courseId} — 🔑 UpdateCourses

Updates the course's title, description, dates, tags, learning outcomes, and prerequisites.

**Request Body**

```json
{
  "title": "Advanced AI",
  "description": "Deep dive into...",
  "startin": "2025-09-01",
  "endin": "2026-01-01",
  "tags": [1, 5],
  "learningOutcomes": ["Master deep learning"],
  "prerequisites": ["Intro to AI course"]
}
```

| Status | Description |
|--------|-------------|
| `204`  | Course updated. |
| `404`  | Course or tags not found. |

---

### `PUT` /api/courses/{courseId}/cover-image — 🔑 UpdateCourses

Uploads or replaces the cover image. Accepts `multipart/form-data`. The old image is deleted unless it's the default placeholder. Allowed formats: `.jpg`, `.jpeg`, `.png`. Max size: 10 MB.

**Form Data**

| Field   | Type | Description               |
|---------|------|---------------------------|
| `image` | file | The image file to upload   |

| Status | Description |
|--------|-------------|
| `204`  | Image updated. |
| `404`  | Course not found. |

---

### `POST` /api/courses/{courseId}/enroll — 🔒 Auth

Enrolls the authenticated user. Idempotent — re-enrolls soft-deleted enrollments.

| Status | Description |
|--------|-------------|
| `204`  | Enrolled successfully. |
| `404`  | Course not found. |

---

### `DELETE` /api/courses/{courseId}/enroll — 🔒 Auth

Soft-deletes the enrollment. Course owners cannot unenroll.

| Status | Description |
|--------|-------------|
| `204`  | Unenrolled. |
| `400`  | User is the course owner. |
| `404`  | Not enrolled. |

---

### `POST` /api/courses/{courseId}/bookmark — 🔒 Auth

Adds a bookmark if none exists; removes it if it does. Soft-deleted bookmarks are restored.

| Status | Description |
|--------|-------------|
| `204`  | Bookmark toggled. |
| `404`  | Course not found. |

---

## 📑 Sections

### `GET` /api/courses/{courseId}/sections — `Public`

Returns all sections for a specific course.

| Status | Description |
|--------|-------------|
| `200`  | Returns `IEnumerable<SectionResponse>` |
| `404`  | Course not found. |

---

### `GET` /api/sections/{sectionId} — 🔒 Auth

Returns a specific section by ID.

| Status | Description |
|--------|-------------|
| `200`  | Returns `SectionResponse` |
| `404`  | Section not found. |

---

### `POST` /api/courses/{courseId}/sections — 🔒 Auth

Creates a new section within a course.

**Request Body**

```json
{
  "title": "Getting Started",
  "description": "Introduction to the module",
  "position": 1
}
```

| Status | Description |
|--------|-------------|
| `201`  | Section created. `Location` header included. |
| `400`  | Validation error. |
| `404`  | Course not found. |

---

### `PUT` /api/sections/{sectionId} — 🔒 Auth

Updates a section's details.

| Status | Description |
|--------|-------------|
| `204`  | Section updated. |
| `404`  | Section not found. |

---

### `PUT` /api/sections/{sectionId}/status — 🔒 Auth

Toggles section publish/unpublish status.

| Status | Description |
|--------|-------------|
| `204`  | Status toggled. |
| `404`  | Section not found. |

---

## 🎬 Lessons

### `POST` /api/lessons/init — 🔒 Auth

Creates the lesson shell (step 1 of the two-step wizard). Returns the new lesson ID.

**Request Body**

```json
{
  "title": "What is a Neural Network?",
  "sectionId": 5,
  "type": 1       // 1 = Video, 2 = Article, 3 = Quiz
}
```

| Status | Description |
|--------|-------------|
| `200`  | Returns `{ lessonId: int }` |
| `400`  | Validation error. |

---

### `PUT` /api/lessons/{id}/complete — 🔒 Auth

Step 2: uploads the video file and completes the lesson. Accepts `multipart/form-data`. Max upload: 1 GB.

**Form Data**

| Field           | Type      | Description                             |
|-----------------|-----------|-----------------------------------------|
| `description`   | string?   | Lesson description                      |
| `isPreview`     | bool      | Whether the lesson is a free preview    |
| `scheduledDate` | DateTime? | Scheduled publish date (must be future) |
| `videoFile`     | file?     | The video file                          |

| Status | Description |
|--------|-------------|
| `204`  | Lesson completed. |
| `404`  | Lesson not found. |

---

### `GET` /api/lessons/{id}/stream — 🔒 Auth

Streams the lesson video file. Supports HTTP range requests for seeking. Response is not cached.

| Status | Description |
|--------|-------------|
| `200`  | Video file stream (`video/*` content type). |
| `404`  | Lesson or video not found. |

---

## ⭐ Reviews

### `POST` /api/courses/{courseId}/reviews — 🔒 Auth

Adds a review or updates an existing one. The user must be enrolled and cannot review their own course.

**Request Body**

```json
{
  "rating": 5,
  "comment": "Excellent course!"
}
```

| Status | Description |
|--------|-------------|
| `204`  | Review saved. |
| `400`  | Invalid rating (must be 1–5), not enrolled, or reviewing own course. |
| `404`  | Course not found. |

---

### `GET` /api/reviews/course/{courseId} — `Public`

Returns paginated reviews for a specific course.

**Query Parameters**

| Name       | Type | Default | Description    |
|------------|------|---------|----------------|
| `page`     | int  | 1       | Page number    |
| `pageSize` | int  | 5       | Items per page |

| Status | Description |
|--------|-------------|
| `200`  | Returns paginated reviews. |
| `404`  | Course not found. |

---

## 📢 Announcements

### `GET` /api/announcements/posts — `Public`

Returns a paginated list of announcement posts.

**Query Parameters**

| Name         | Type | Default |
|--------------|------|---------|
| `pageNumber` | int  | 1       |
| `pageSize`   | int  | 10      |

| Status | Description |
|--------|-------------|
| `200`  | Returns paginated posts. |

---

### `GET` /api/announcements/posts/{postId} — `Public`

Returns a specific post by ID.

| Status | Description |
|--------|-------------|
| `200`  | Returns `PostResponse` |
| `404`  | Post not found. |

---

### `POST` /api/announcements/posts — 🔒 Auth

Creates a new post. Accepts `multipart/form-data` (text + optional image).

| Status | Description |
|--------|-------------|
| `201`  | Post created. |
| `400`  | Validation error. |

---

### `PUT` /api/announcements/posts/{postId} — 🔒 Auth

Updates a post's content.

| Status | Description |
|--------|-------------|
| `200`  | Returns updated post. |
| `404`  | Post not found or not the author. |

---

### `DELETE` /api/announcements/posts/{postId} — 🔒 Auth

Deletes a post (soft-delete).

| Status | Description |
|--------|-------------|
| `204`  | Post deleted. |
| `404`  | Post not found or not the author. |

---

### `PUT` /api/announcements/posts/{postId}/visibility — 🔒 Auth

Toggles the visibility of a post.

| Status | Description |
|--------|-------------|
| `204`  | Visibility toggled. |

---

### `PUT` /api/announcements/posts/{postId}/image — 🔒 Auth

Updates the post image. Accepts `multipart/form-data`.

| Status | Description |
|--------|-------------|
| `204`  | Image updated. |

---

### `POST` /api/announcements/posts/{postId}/likes — 🔒 Auth

Toggles a like on the post.

| Status | Description |
|--------|-------------|
| `204`  | Like toggled. |

---

### `POST` /api/announcements/posts/{postId}/comments — 🔒 Auth

Adds a comment to a post. Accepts `multipart/form-data` (text + optional image).

| Status | Description |
|--------|-------------|
| `201`  | Comment created. |

---

### `PUT` /api/announcements/comments/{commentId} — 🔒 Auth

Updates a comment's content.

| Status | Description |
|--------|-------------|
| `200`  | Returns updated comment. |

---

### `PUT` /api/announcements/comments/{commentId}/image — 🔒 Auth

Updates the comment image. Accepts `multipart/form-data`.

| Status | Description |
|--------|-------------|
| `204`  | Image updated. |

---

### `DELETE` /api/announcements/comments/{commentId} — 🔒 Auth

Deletes a comment.

| Status | Description |
|--------|-------------|
| `204`  | Comment deleted. |

---

## 👥 Users

### `GET` /api/users/course/{courseId} — `Public`

Returns the instructor summary for the given course, including total students, rating, and course count.

| Status | Description |
|--------|-------------|
| `200`  | Returns `InstructorSummaryResponse` |
| `404`  | Course not found. |

---

> **Neura API** — Generated from source code — .NET 10

using Neura.Core.Enums;

namespace Neura.Core.Contracts.Lessons;

/// <summary>
///     Request to update lesson position within its section.
/// </summary>
public record UpdateLessonPositionRequest(
    int NewPosition // 1-based position in the section
);

/// <summary>
///     Request to change lesson privacy status.
/// </summary>
public record UpdateLessonPrivacyRequest(
    bool IsVideoPrivate, // true = private (enrolled only), false = public preview
    bool IsPreview, // true = preview mode
    bool IsPubliclyVisible
);

/// <summary>
///     Request to update lesson basic information.
/// </summary>
public record UpdateLessonRequest(
    string? Title,
    string? Description,
    bool IsPreview,
    DateTime? ScheduledDate
);

/// <summary>
///     Response containing lesson with position information.
/// </summary>
public record LessonWithPositionResponse(
    int Id,
    string Title,
    string? Description,
    int Position, // Position in the section (1-based)
    int TotalInSection, // Total lessons in section
    bool IsPreview,
    bool IsVideoPrivate,
    bool IsPublished,
    LessonType Type,
    string? VideoUrl,
    DateTime? CreatedAt
);

/// <summary>
///     Response for reordering operation - returns new positions of affected lessons.
/// </summary>
public record LessonReorderResponse(
    int LessonId,
    int NewPosition,
    int OldPosition
);
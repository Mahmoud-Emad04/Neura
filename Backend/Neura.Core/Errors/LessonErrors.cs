using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class LessonErrors
{
    // 1. EXISTENCE
    public static readonly Error NotFound = new(
        "Lesson.NotFound",
        "The requested lesson was not found.",
        StatusCodes.Status404NotFound);

    // 2. VIDEO & FILE HANDLING
    public static readonly Error VideoNotFound = new(
        "Lesson.VideoNotFound",
        "This lesson is marked as a video, but no video file is associated with it.",
        StatusCodes.Status404NotFound);

    public static readonly Error FileNotFound = new(
        "Lesson.FileNotFound",
        "The physical video file could not be found on the server.",
        StatusCodes.Status500InternalServerError);

    public static readonly Error InvalidVideoFormat = new(
        "Lesson.InvalidVideoFormat",
        "Only .mp4 and .webm formats are supported.",
        StatusCodes.Status400BadRequest);

    public static readonly Error VideoRequired = new(
        "Lesson.VideoRequired",
        "A video file is required for this lesson type.",
        StatusCodes.Status400BadRequest);

    // 3. ACCESS CONTROL & SCHEDULING
    public static readonly Error NotEnrolled = new(
        "Lesson.NotEnrolled",
        "You must be enrolled in the course to view this lesson.",
        StatusCodes.Status403Forbidden);

    public static readonly Error NotAvailableYet = new(
        "Lesson.NotAvailableYet",
        "This lesson is scheduled for a future date and is not yet available.",
        StatusCodes.Status403Forbidden);

    public static readonly Error PreviewNotAllowed = new(
        "Lesson.PreviewNotAllowed",
        "This lesson is not available for free preview.",
        StatusCodes.Status403Forbidden);
}

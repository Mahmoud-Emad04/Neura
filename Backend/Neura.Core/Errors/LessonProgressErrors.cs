using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class LessonProgressErrors
{
    public static readonly Error LessonNotFound =
        new("LessonProgress.LessonNotFound", "Lesson not found.", StatusCodes.Status404NotFound);

    public static readonly Error NotEnrolled =
        new("LessonProgress.NotEnrolled",
            "You must be enrolled in this course to track progress on non-preview lessons.",
            StatusCodes.Status403Forbidden);

    public static readonly Error LessonNotAccessible =
        new("LessonProgress.LessonNotAccessible",
            "This lesson is not currently accessible.", StatusCodes.Status403Forbidden);

    public static readonly Error CourseNotFound =
        new("LessonProgress.CourseNotFound", "Course not found.", StatusCodes.Status404NotFound);
}
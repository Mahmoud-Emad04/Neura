using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class CourseErrors
{
    public static readonly Error CourseNotFound =
        new("Course.NotFound", "The specified Course was not found.", StatusCodes.Status404NotFound);

    public static readonly Error CourseTagNotFound =
        new("Course.TagNotFound", "One or more provided tag IDs do not exist.", StatusCodes.Status404NotFound);

    public static readonly Error CourseInvalidData =
        new("Course.InvalidData", "One or more course fields are invalid.", StatusCodes.Status400BadRequest);

    public static readonly Error UserAlreadyEnrolled =
        new("Course.UserAlreadyEnrolled", "The user is already enrolled in this course.",
            StatusCodes.Status409Conflict);

    public static readonly Error UserNotEnrolled =
        new("Course.UserNotEnrolled", "The user is not enrolled in this course.", StatusCodes.Status404NotFound);

    public static readonly Error OwnerCannotUnenroll =
        new("Course.OwnerCannotUnenroll",
            "The course owner cannot unenroll. You must transfer ownership or delete the course.",
            StatusCodes.Status400BadRequest);

    public static readonly Error NotEnrolled = new(
        "Course.NotEnrolled",
        "You must be enrolled in this course to perform this action.",
        StatusCodes.Status403Forbidden
    );
}
using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class CourseErrors
{
    // ══════════════════════════════════════════════════════════════
    // Not Found (404)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error CourseNotFound =
        new("Course.NotFound", "The specified course was not found.", StatusCodes.Status404NotFound);

    public static readonly Error TagNotFound =
        new("Course.TagNotFound", "One or more provided tag IDs do not exist.", StatusCodes.Status404NotFound);

    public static readonly Error UserNotFound =
        new("Course.UserNotFound", "The specified user was not found.", StatusCodes.Status404NotFound);

    public static readonly Error UserNotEnrolled =
        new("Course.UserNotEnrolled", "The user is not enrolled in this course.", StatusCodes.Status404NotFound);

    public static readonly Error CourseTagNotFound =
        new("Course.TagNotFound", "One or more provided tag IDs do not exist.", StatusCodes.Status404NotFound);

    // ══════════════════════════════════════════════════════════════
    // Unauthorized (401)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error UnauthorizedAction =
        new("Course.UnauthorizedAction",
            "You do not have the required permissions to perform this action on this course.",
            StatusCodes.Status401Unauthorized);

    // ══════════════════════════════════════════════════════════════
    // Forbidden (403)
    // ══════════════════════════════════════════════════════════════
    public static readonly Error CourseNotAcceptingEnrollments =
        new("Course.Status.NotAcceptingEnrollments", "This course is not currently accepting new enrollments.",
            StatusCodes.Status403Forbidden);

    public static readonly Error CourseNotAccessible =
        new("Course.Status.NotAccessible", "This course is not currently accessible.", StatusCodes.Status403Forbidden);

    public static readonly Error NotEnrolled =
        new("Course.NotEnrolled", "You must be enrolled in this course to perform this action.",
            StatusCodes.Status403Forbidden);

    public static readonly Error AccessDenied =
        new("Course.AccessDenied", "You do not have permission to access this course.", StatusCodes.Status403Forbidden);

    // ══════════════════════════════════════════════════════════════
    // Bad Request (400)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error InvalidData =
        new("Course.InvalidData", "One or more course fields are invalid.", StatusCodes.Status400BadRequest);

    public static readonly Error InvalidId =
        new("Course.InvalidId", "The provided course ID is invalid.", StatusCodes.Status400BadRequest);

    public static readonly Error OwnerCannotUnenroll =
        new("Course.OwnerCannotUnenroll",
            "The course owner cannot unenroll. You must transfer ownership or delete the course.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidDateRange =
        new("Course.InvalidDateRange", "End date must be after start date.", StatusCodes.Status400BadRequest);

    public static readonly Error InvalidPrice =
        new("Course.InvalidPrice", "Price must be zero or greater.", StatusCodes.Status400BadRequest);

    public static readonly Error CourseInvalidData =
        new("Course.InvalidData", "One or more course fields are invalid.", StatusCodes.Status400BadRequest);

    public static readonly Error CanOnlyActivateFromPending =
        new("Course.Status.InvalidActivation", "Course can only be activated from Pending status.",
            StatusCodes.Status400BadRequest);

    public static readonly Error CanOnlyCompleteFromActive =
        new("Course.Status.InvalidCompletion", "Only active courses can be marked as completed.",
            StatusCodes.Status400BadRequest);

    public static readonly Error CanOnlyReactivateFromCompleted =
        new("Course.Status.InvalidReactivation", "Only completed courses can be reactivated.",
            StatusCodes.Status400BadRequest);

    public static readonly Error CanOnlyUnpublishFromActive =
        new("Course.Status.InvalidUnpublish", "Only active courses can be unpublished.",
            StatusCodes.Status400BadRequest);

    public static readonly Error EnrollmentClosed =
        new("Course.EnrollmentClosed", "Enrollment is currently closed for this course",
            StatusCodes.Status400BadRequest);

    public static readonly Error CourseNotPublished =
        new("Course.NotPublished", "This course is not published yet", StatusCodes.Status400BadRequest);

    public static readonly Error TeamMemberCannotSelfUnenroll =
        new("Course.TeamMemberCannotSelfUnenroll", "Team members must be removed by the course owner",
            StatusCodes.Status400BadRequest);

    public static readonly Error CourseNotActive =
        new("Course.NotActive", "This course is not currently available for enrollment",
            StatusCodes.Status400BadRequest);
    // ══════════════════════════════════════════════════════════════
    // Conflict (409)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error UserAlreadyEnrolled =
        new("Course.UserAlreadyEnrolled", "The user is already enrolled in this course.",
            StatusCodes.Status409Conflict);

    public static readonly Error DuplicateTitle =
        new("Course.DuplicateTitle", "A course with this title already exists.", StatusCodes.Status409Conflict);

    // Business Rule Violations (422)
    public static readonly Error CourseHasNoPublishedContent =
        new("Course.Status.NoPublishedContent", "Course must have at least one published lesson before activation.",
            StatusCodes.Status422UnprocessableEntity);
}
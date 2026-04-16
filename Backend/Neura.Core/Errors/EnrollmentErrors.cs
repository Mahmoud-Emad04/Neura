using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class EnrollmentErrors
{
    public static readonly Error CourseNotFound =
        new("Enrollment.CourseNotFound", "Course not found", StatusCodes.Status404NotFound);

    public static readonly Error UserNotFound =
        new("Enrollment.UserNotFound", "User not found", StatusCodes.Status404NotFound);

    public static readonly Error CourseNotActive =
        new("Enrollment.CourseNotActive", "This course is not currently available for enrollment",
            StatusCodes.Status400BadRequest);

    public static readonly Error AlreadyEnrolled =
        new("Enrollment.AlreadyEnrolled", "You are already enrolled in this course", StatusCodes.Status409Conflict);

    public static readonly Error NotEnrolled =
        new("Enrollment.NotEnrolled", "You are not enrolled in this course", StatusCodes.Status404NotFound);

    public static readonly Error EmailNotVerified =
        new("Enrollment.EmailNotVerified", "Please verify your email before enrolling in courses",
            StatusCodes.Status400BadRequest);

    public static readonly Error CannotUnenrollOwner =
        new("Enrollment.CannotUnenrollOwner", "Course owner cannot unenroll from their own course",
            StatusCodes.Status400BadRequest);

    public static readonly Error CannotUnenrollTeamMember =
        new("Enrollment.CannotUnenrollTeamMember", "Team members must be removed by the course owner",
            StatusCodes.Status400BadRequest);

    public static readonly Error CourseRequiresPayment =
        new("Enrollment.CourseRequiresPayment", "This course requires payment", StatusCodes.Status400BadRequest);

    public static readonly Error EnrollmentClosed =
        new("Enrollment.EnrollmentClosed", "Enrollment is currently closed for this course",
            StatusCodes.Status400BadRequest);

    public static readonly Error MaxStudentsReached =
        new("Enrollment.MaxStudentsReached", "This course has reached its maximum number of students",
            StatusCodes.Status400BadRequest);

    public static readonly Error CannotRemoveStudent =
        new("Enrollment.CannotRemoveStudent", "You don't have permission to remove students",
            StatusCodes.Status400BadRequest);

    public static readonly Error StudentNotFound =
        new("Enrollment.StudentNotFound", "Student not found in this course", StatusCodes.Status404NotFound);
}
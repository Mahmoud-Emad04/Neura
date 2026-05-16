using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class InstructorApplicationErrors
{
    public static readonly Error UserNotFound =
        new("InstructorApplication.UserNotFound", "User not found", StatusCodes.Status404NotFound);

    public static readonly Error AlreadyInstructor =
        new("InstructorApplication.AlreadyInstructor", "User is already an instructor", StatusCodes.Status409Conflict);

    public static readonly Error PendingApplicationExists =
        new("InstructorApplication.PendingExists", "You already have a pending application",
            StatusCodes.Status409Conflict);

    public static readonly Error CannotReapplyYet =
        new("InstructorApplication.CannotReapplyYet", "You cannot reapply yet. Please wait until the reapply date",
            StatusCodes.Status400BadRequest);

    public static readonly Error ApplicationNotFound =
        new("InstructorApplication.NotFound", "Application not found", StatusCodes.Status404NotFound);

    public static readonly Error ApplicationAlreadyReviewed =
        new("InstructorApplication.AlreadyReviewed", "This application has already been reviewed",
            StatusCodes.Status409Conflict);

    public static readonly Error InvalidStatus =
        new("InstructorApplication.InvalidStatus", "Invalid application status", StatusCodes.Status400BadRequest);

    public static readonly Error RejectionReasonRequired =
        new("InstructorApplication.RejectionReasonRequired",
            "Rejection reason is required when rejecting an application", StatusCodes.Status400BadRequest);

    public static Error ReapplyDateNotReached(DateTime canReapplyAfter)
    {
        return new Error("InstructorApplication.ReapplyDateNotReached",
            $"You can reapply after {canReapplyAfter:yyyy-MM-dd}",
            StatusCodes.Status400BadRequest);
    }
}
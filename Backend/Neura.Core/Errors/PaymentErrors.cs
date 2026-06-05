using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class PaymentErrors
{
    public static readonly Error CourseNotFound =
        new("Payment.CourseNotFound", "Course not found", StatusCodes.Status404NotFound);

    public static readonly Error UserNotFound =
        new("Payment.UserNotFound", "User not found", StatusCodes.Status404NotFound);

    public static readonly Error CourseIsFree =
        new("Payment.CourseIsFree", "This course is free and does not require payment", StatusCodes.Status400BadRequest);

    public static readonly Error AlreadyEnrolled =
        new("Payment.AlreadyEnrolled", "You are already enrolled in this course", StatusCodes.Status409Conflict);

    public static readonly Error PaymentPending =
        new("Payment.PaymentPending", "You already have a pending payment for this course", StatusCodes.Status409Conflict);

    public static readonly Error CourseNotActive =
        new("Payment.CourseNotActive", "This course is not currently available for enrollment",
            StatusCodes.Status400BadRequest);

    public static readonly Error EmailNotVerified =
        new("Payment.EmailNotVerified", "Please verify your email before purchasing courses",
            StatusCodes.Status400BadRequest);

    public static readonly Error StripeError =
        new("Payment.StripeError", "An error occurred processing the payment", StatusCodes.Status502BadGateway);

    public static readonly Error WebhookError =
        new("Payment.WebhookError", "Invalid webhook signature", StatusCodes.Status400BadRequest);
}

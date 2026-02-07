using Neura.Core.Abstractions;

// Or wherever your 'Error' record lives

namespace Neura.Core.Errors;

public static class ReviewErrors
{
    public static readonly Error ReviewNotFound = new(
        "Review.NotFound",
        "The review was not found.",
        StatusCodes.Status404NotFound
    );

    public static readonly Error InvalidRating = new(
        "Review.InvalidRating",
        "The rating must be between 1 and 5 stars.",
        StatusCodes.Status400BadRequest
    );

    public static readonly Error ReviewAlreadyExists = new(
        "Review.AlreadyExists",
        "You have already reviewed this course. Please update your existing review instead.",
        StatusCodes.Status409Conflict
    );

    public static readonly Error NotEligible = new(
        "Review.NotEligible",
        "You must be enrolled in the course to leave a review.",
        StatusCodes.Status403Forbidden
    );

    public static readonly Error NotEnrolled = new(
        "Review.NotEnrolled", "You must be enrolled in the course to write a review.", StatusCodes.Status403Forbidden);

    public static readonly Error CannotReviewOwnCourse = new(
        "Review.ConflictOfInterest", "You cannot review your own course.", StatusCodes.Status409Conflict);

    public static readonly Error AlreadyReviewed = new(
        "Review.Duplicate", "You have already reviewed this course.", StatusCodes.Status409Conflict);
}
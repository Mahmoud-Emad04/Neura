using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class ExamAttemptErrors
{
    // ══════════════════════════════════════════════════════════════
    // Not Found (404)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error AttemptNotFound =
        new("Attempt.NotFound", "The specified exam attempt was not found.", StatusCodes.Status404NotFound);

    public static readonly Error ExamNotFound =
        new("Attempt.ExamNotFound", "The specified exam was not found.", StatusCodes.Status404NotFound);

    public static readonly Error QuestionNotInAttempt =
        new("Attempt.QuestionNotInAttempt",
            "This question was not served in your attempt.",
            StatusCodes.Status404NotFound);

    // ══════════════════════════════════════════════════════════════
    // Bad Request (400)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error ExamNotPublished =
        new("Attempt.ExamNotPublished", "This exam is not yet available.", StatusCodes.Status400BadRequest);

    public static readonly Error MaxAttemptsReached =
        new("Attempt.MaxAttemptsReached",
            "You have reached the maximum number of attempts for this exam.",
            StatusCodes.Status400BadRequest);

    public static readonly Error AttemptAlreadyInProgress =
        new("Attempt.AlreadyInProgress",
            "You already have an in-progress attempt for this exam. Please complete or submit it first.",
            StatusCodes.Status400BadRequest);

    public static readonly Error AttemptNotInProgress =
        new("Attempt.NotInProgress",
            "This attempt is no longer in progress and cannot be modified.",
            StatusCodes.Status400BadRequest);

    public static readonly Error AttemptTimedOut =
        new("Attempt.TimedOut",
            "Time has expired for this attempt. It has been automatically submitted.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidSelectedOptions =
        new("Attempt.InvalidSelectedOptions",
            "One or more selected options do not belong to this question.",
            StatusCodes.Status400BadRequest);

    public static readonly Error SingleChoiceMultipleSelections =
        new("Attempt.SingleChoiceMultipleSelections",
            "Single choice and True/False questions can only have one selected option.",
            StatusCodes.Status400BadRequest);

    public static readonly Error NoOptionsSelected =
        new("Attempt.NoOptionsSelected",
            "You must select at least one option.",
            StatusCodes.Status400BadRequest);

    public static readonly Error ResultsNotAvailable =
        new("Attempt.ResultsNotAvailable",
            "Results are only available after the attempt has been graded.",
            StatusCodes.Status400BadRequest);

    // ══════════════════════════════════════════════════════════════
    // Forbidden (403)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error NotEnrolled =
        new("Attempt.NotEnrolled",
            "You must be enrolled in this course to take this exam.",
            StatusCodes.Status403Forbidden);

    public static readonly Error NotAttemptOwner =
        new("Attempt.NotAttemptOwner",
            "You do not have permission to access this attempt.",
            StatusCodes.Status403Forbidden);

    // ══════════════════════════════════════════════════════════════
    // Violation Workflow
    // ══════════════════════════════════════════════════════════════

    public static readonly Error AttemptNotGraded =
        new("Attempt.NotGraded",
            "Only graded attempts can be flagged for violation.",
            StatusCodes.Status400BadRequest);

    public static readonly Error AttemptNotFlagged =
        new("Attempt.NotFlagged",
            "Only violation-flagged attempts can be resolved.",
            StatusCodes.Status400BadRequest);

    public static readonly Error ViolationReasonRequired =
        new("Attempt.ViolationReasonRequired",
            "A reason for the violation is required.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InstructorNotesRequired =
        new("Attempt.InstructorNotesRequired",
            "Instructor notes are required when resolving a violation.",
            StatusCodes.Status400BadRequest);

    public static readonly Error GradesNotPublished =
        new("Attempt.GradesNotPublished",
            "Grades are not yet available for this exam.",
            StatusCodes.Status403Forbidden);
}
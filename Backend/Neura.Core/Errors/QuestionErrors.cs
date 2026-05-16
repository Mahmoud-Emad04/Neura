using Neura.Core.Abstractions;

namespace Neura.Core.Errors;


public static class QuestionErrors
{
    // ══════════════════════════════════════════════════════════════
    // Not Found (404)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error QuestionNotFound =
        new("Question.NotFound", "The specified question was not found.", StatusCodes.Status404NotFound);

    // ══════════════════════════════════════════════════════════════
    // Bad Request (400)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error CannotDeleteWithAttempts =
        new("Question.CannotDeleteWithAttempts",
            "Cannot delete a question that has student answers.",
            StatusCodes.Status400BadRequest);

    public static readonly Error CannotChangeQuestionType =
        new("Question.CannotChangeQuestionType",
            "Cannot change the question type after students have answered it.",
            StatusCodes.Status400BadRequest);

    public static readonly Error CannotChangeCorrectAnswers =
        new("Question.CannotChangeCorrectAnswers",
            "Cannot change correct answers after students have answered this question.",
            StatusCodes.Status400BadRequest);

    public static readonly Error CannotRemoveSelectedOptions =
        new("Question.CannotRemoveSelectedOptions",
            "Cannot remove answer options that students have already selected.",
            StatusCodes.Status400BadRequest);

    public static readonly Error CannotAddOptionsAfterAttempts =
        new("Question.CannotAddOptionsAfterAttempts",
            "Cannot add new options to a question after students have answered it.",
            StatusCodes.Status400BadRequest);

    public static readonly Error ReorderIdsMismatch =
        new("Question.ReorderIdsMismatch",
            "The provided question IDs don't match the exam's questions.",
            StatusCodes.Status400BadRequest);

    public static readonly Error ReorderDuplicateIds =
        new("Question.ReorderDuplicateIds",
            "Duplicate question IDs in the reorder request.",
            StatusCodes.Status400BadRequest);

    // ══════════════════════════════════════════════════════════════
    // Forbidden (403)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error Forbidden =
        new("Question.Forbidden", "You do not have permission to manage this question.", StatusCodes.Status403Forbidden);
}
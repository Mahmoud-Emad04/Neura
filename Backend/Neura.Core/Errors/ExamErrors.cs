using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class ExamErrors
{
    // ══════════════════════════════════════════════════════════════
    // Not Found (404)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error ExamNotFound =
        new("Exam.NotFound", "The specified exam was not found.", StatusCodes.Status404NotFound);

    public static readonly Error LessonNotFound =
        new("Exam.LessonNotFound", "The specified lesson was not found.", StatusCodes.Status404NotFound);

    public static readonly Error NoExamForLesson =
        new("Exam.NoExamForLesson", "No exam found for this lesson.", StatusCodes.Status404NotFound);
    public static readonly Error InvalidExamId =
    new("Exam.InvalidId", "Invalid exam ID format", StatusCodes.Status400BadRequest);
    // ══════════════════════════════════════════════════════════════
    // Bad Request (400)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error LessonNotQuizType =
        new("Exam.LessonNotQuizType", "Exams can only be created for lessons of type Quiz.", StatusCodes.Status400BadRequest);

    public static readonly Error AlreadyPublished =
        new("Exam.AlreadyPublished", "This exam is already published.", StatusCodes.Status400BadRequest);

    public static readonly Error AlreadyUnpublished =
        new("Exam.AlreadyUnpublished", "This exam is already unpublished.", StatusCodes.Status400BadRequest);

    public static readonly Error NoQuestions =
        new("Exam.NoQuestions", "Cannot publish an exam with no questions.", StatusCodes.Status400BadRequest);

    public static readonly Error QuestionsWithoutCorrectAnswer =
        new("Exam.QuestionsWithoutCorrectAnswer",
            "One or more questions have no correct answer. All questions must have at least one correct answer before publishing.",
            StatusCodes.Status400BadRequest);

    public static readonly Error PoolSizeExceedsTotalQuestions =
        new("Exam.PoolSizeExceedsTotalQuestions",
            "NumberOfQuestionsToServe cannot exceed the total number of questions.",
            StatusCodes.Status400BadRequest);

    public static readonly Error CannotDeleteWithAttempts =
        new("Exam.CannotDeleteWithAttempts",
            "Cannot delete an exam that has student attempts.",
            StatusCodes.Status400BadRequest);

    public static readonly Error CannotUnpublishWithAttempts =
        new("Exam.CannotUnpublishWithAttempts",
            "Cannot unpublish an exam that has student attempts.",
            StatusCodes.Status400BadRequest);

    // ══════════════════════════════════════════════════════════════
    // Conflict (409)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error ExamAlreadyExists =
        new("Exam.AlreadyExists", "This lesson already has an exam.", StatusCodes.Status409Conflict);

    // ══════════════════════════════════════════════════════════════
    // Forbidden (403)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error Forbidden =
        new("Exam.Forbidden", "You do not have permission to manage this exam.", StatusCodes.Status403Forbidden);

    // ══════════════════════════════════════════════════════════════
    // Grade Publishing
    // ══════════════════════════════════════════════════════════════

    public static readonly Error GradesAlreadyPublished =
        new("Exam.GradesAlreadyPublished",
            "Grades for this exam are already published.", StatusCodes.Status400BadRequest);

    public static readonly Error GradesAlreadyHidden =
        new("Exam.GradesAlreadyHidden",
            "Grades for this exam are already hidden.", StatusCodes.Status400BadRequest);
}
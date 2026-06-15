using Neura.Core.Enums;

namespace Neura.Core.Entities;

public class Question
{
    public int Id { get; set; }
    public int ExamId { get; set; }

    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public QuestionLevel Level { get; set; } = QuestionLevel.Easy;
    public decimal Points { get; set; }
    public int Order { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Exam Exam { get; set; } = null!;
    public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
    public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();
}
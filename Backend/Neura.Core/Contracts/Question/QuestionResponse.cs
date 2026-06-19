using Neura.Core.Enums;

namespace Neura.Core.Contracts.Question;


public class QuestionResponse
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public QuestionLevel Level { get; set; }
    public decimal Points { get; set; }
    public int Order { get; set; }
    public bool HasAttempts { get; set; }
    public List<AnswerOptionResponse> Options { get; set; } = new();
}

public class AnswerOptionResponse
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}
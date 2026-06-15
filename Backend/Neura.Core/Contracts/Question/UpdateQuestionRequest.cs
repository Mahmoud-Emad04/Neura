using Neura.Core.Enums;

namespace Neura.Core.Contracts.Question;

public class UpdateQuestionRequest
{
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public QuestionLevel Level { get; set; } = QuestionLevel.Easy;
    public decimal Points { get; set; }
    public List<UpdateAnswerOptionRequest> Options { get; set; } = new();
}

public class UpdateAnswerOptionRequest
{
    public int? Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
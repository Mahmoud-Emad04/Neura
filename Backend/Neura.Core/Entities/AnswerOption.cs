namespace Neura.Core.Entities;


public class AnswerOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }

    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }

    // Navigation
    public Question Question { get; set; } = null!;
    public ICollection<AttemptAnswerOption> AttemptAnswerOptions { get; set; } = new List<AttemptAnswerOption>();
}
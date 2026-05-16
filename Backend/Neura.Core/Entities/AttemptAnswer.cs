namespace Neura.Core.Entities;

public class AttemptAnswer
{
    public int Id { get; set; }
    public int ExamAttemptId { get; set; }
    public int QuestionId { get; set; }

    // Navigation
    public ExamAttempt ExamAttempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public ICollection<AttemptAnswerOption> SelectedOptions { get; set; } = new List<AttemptAnswerOption>();
}
namespace Neura.Core.Entities;

public class AttemptAnswerOption
{
    public int AttemptAnswerId { get; set; }
    public int AnswerOptionId { get; set; }

    // Navigation
    public AttemptAnswer AttemptAnswer { get; set; } = null!;
    public AnswerOption AnswerOption { get; set; } = null!;
}
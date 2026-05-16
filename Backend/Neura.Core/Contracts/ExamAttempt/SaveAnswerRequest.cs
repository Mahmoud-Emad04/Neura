namespace Neura.Core.Contracts.ExamAttempt;

public class SaveAnswerRequest
{
    public List<int> SelectedOptionIds { get; set; } = new();
}
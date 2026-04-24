namespace Neura.Core.Contracts.Question;

public class ReorderQuestionsRequest
{
    public List<int> OrderedQuestionIds { get; set; } = new();
}
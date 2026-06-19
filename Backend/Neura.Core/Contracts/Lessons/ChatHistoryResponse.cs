namespace Neura.Core.Contracts.Lessons;

public class ChatHistoryResponse
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

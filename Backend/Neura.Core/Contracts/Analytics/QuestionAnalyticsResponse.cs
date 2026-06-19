using Neura.Core.Enums;

namespace Neura.Core.Contracts.Analytics;

public class QuestionAnalyticsResponse
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public QuestionLevel Level { get; set; }
    public decimal Points { get; set; }
    public int Order { get; set; }

    public int TotalAnswered { get; set; }
    public int TotalSkipped { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public decimal AccuracyPercentage { get; set; } // CorrectCount / TotalAnswered * 100

    // Per-option distribution — shows which options students chose
    public List<OptionAnalyticsResponse> Options { get; set; } = new();
}

public class OptionAnalyticsResponse
{
    public int OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SelectionCount { get; set; }
    public decimal SelectionPercentage { get; set; } // Out of TotalAnswered
}
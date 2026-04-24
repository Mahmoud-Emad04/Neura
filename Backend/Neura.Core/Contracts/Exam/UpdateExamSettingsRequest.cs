namespace Neura.Core.Contracts.Exam;

public class UpdateExamSettingsRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DurationInMinutes { get; set; }
    public decimal PassingScorePercentage { get; set; }
    public int? MaxAttempts { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public int? NumberOfQuestionsToServe { get; set; }
    public bool EnableTabSwitchDetection { get; set; }
    public int? MaxViolationsBeforeAutoSubmit { get; set; }
}
namespace Neura.Core.Contracts.Exam;


public class ExamResponse
{
    public int Id { get; set; }
    public int LessonId { get; set; }
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
    public bool IsPublished { get; set; }
    public bool AreGradesPublished { get; set; }
    public int TotalQuestions { get; set; }
    public decimal TotalPoints { get; set; }
    public int TotalAttempts { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
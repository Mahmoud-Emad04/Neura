namespace Neura.Core.Entities;
//TODO ADD : AuditableEntity
public class Exam : AuditableEntity
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

    public bool ShowCorrectAnswersAfterSubmit { get; set; } = true;
    public bool IsPublished { get; set; }

    // Navigation
    public Lesson Lesson { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<ExamAttempt> Attempts { get; set; } = new List<ExamAttempt>();
}
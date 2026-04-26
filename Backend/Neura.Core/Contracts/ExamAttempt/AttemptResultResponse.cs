using Neura.Core.Enums;

namespace Neura.Core.Contracts.ExamAttempt;

public class AttemptResultResponse
{
    public int AttemptId { get; set; }
    public decimal Score { get; set; }
    public decimal ScorePercentage { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal PassingScorePercentage { get; set; }
    public bool Passed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public int Unanswered { get; set; }
    public int ViolationCount { get; set; }

    public List<QuestionResultResponse> Questions { get; set; } = new();
}

public class QuestionResultResponse
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public decimal Points { get; set; }
    public decimal EarnedPoints { get; set; }
    public bool IsCorrect { get; set; }
    public bool IsAnswered { get; set; }
    public List<OptionResultResponse> Options { get; set; } = new();
}

public class OptionResultResponse
{
    public int OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }          // ✅ Now revealed — exam is over
    public bool WasSelected { get; set; }
}
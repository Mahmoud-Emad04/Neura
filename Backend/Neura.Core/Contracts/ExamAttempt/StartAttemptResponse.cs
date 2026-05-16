using Neura.Core.Enums;

namespace Neura.Core.Contracts.ExamAttempt;

public class StartAttemptResponse
{
    public int AttemptId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }        // StartedAt + Duration (null = no limit)
    public bool EnableTabSwitchDetection { get; set; }
    public int? MaxViolationsBeforeAutoSubmit { get; set; }
    public List<AttemptQuestionResponse> Questions { get; set; } = new();
}

public class AttemptQuestionResponse
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public decimal Points { get; set; }
    public int Order { get; set; }
    public List<AttemptOptionResponse> Options { get; set; } = new();

    // Pre-populated if student already answered (for resume / auto-save display)
    public List<int>? SavedOptionIds { get; set; }
}

public class AttemptOptionResponse
{
    public int OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    // ⚠️ NO IsCorrect — NEVER exposed to students during attempt
}
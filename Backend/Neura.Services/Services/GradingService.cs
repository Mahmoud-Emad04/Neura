using Neura.Core.Enums;
using System.Text.Json;

namespace Neura.Services.Services;


public class GradingService : IGradingService
{
    private readonly ApplicationDbContext _context;

    public GradingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task GradeAttemptAsync(ExamAttempt attempt, AttemptStatus status)
    {
        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();

        var questions = await _context.Questions
            .AsNoTracking()
            .Include(q => q.AnswerOptions)
            .Where(q => questionOrder.Contains(q.Id))
            .ToListAsync();

        var questionLookup = questions.ToDictionary(q => q.Id);

        // Ensure answers are loaded
        if (!attempt.AttemptAnswers.Any())
        {
            await _context.Entry(attempt)
                .Collection(a => a.AttemptAnswers)
                .Query()
                .Include(aa => aa.SelectedOptions)
                .LoadAsync();
        }

        var answerLookup = attempt.AttemptAnswers
            .ToDictionary(
                aa => aa.QuestionId,
                aa => aa.SelectedOptions.Select(so => so.AnswerOptionId).ToHashSet()
            );

        decimal totalScore = 0;
        decimal totalPossible = 0;

        foreach (var qId in questionOrder)
        {
            if (!questionLookup.TryGetValue(qId, out var question))
                continue;

            totalPossible += question.Points;

            var selectedOptionIds = answerLookup.GetValueOrDefault(qId);
            if (selectedOptionIds is null || !selectedOptionIds.Any())
                continue;

            var correctOptionIds = question.AnswerOptions
                .Where(a => a.IsCorrect)
                .Select(a => a.Id)
                .ToHashSet();

            // All-or-Nothing grading
            if (selectedOptionIds.SetEquals(correctOptionIds))
                totalScore += question.Points;
        }

        var scorePercentage = totalPossible > 0
            ? Math.Round((totalScore / totalPossible) * 100, 2)
            : 0;

        attempt.Score = totalScore;
        attempt.ScorePercentage = scorePercentage;
        attempt.Passed = scorePercentage >= attempt.Exam.PassingScorePercentage;
        attempt.Status = status;
        attempt.SubmittedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
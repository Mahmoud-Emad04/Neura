using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using Neura.Repository.Persistence;
using System.Text.Json;

namespace Neura.Api.Features.ExamAnalytics;

public static class ExamAnalyticsHelpers
{
    public static async Task<bool> HasInstructorPermissionAsync(
        ApplicationDbContext context, int courseId, string userId)
    {
        var courseUser = await context.CourseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId);

        if (courseUser is null) return false;

        return ((courseUser.PermissionMask & CoursePermissionMasks.CoInstructor) == CoursePermissionMasks.CoInstructor)
            || ((courseUser.PermissionMask & CoursePermissionMasks.CourseOwner) == CoursePermissionMasks.CourseOwner);
    }

    public static decimal CalculateMedian(List<decimal> sorted)
    {
        if (!sorted.Any()) return 0;
        var count = sorted.Count;
        if (count % 2 == 0)
            return Math.Round((sorted[count / 2 - 1] + sorted[count / 2]) / 2, 2);
        return sorted[count / 2];
    }

    public static async Task<List<QuestionAnalyticsResponse>> BuildQuestionAnalyticsAsync(
        ApplicationDbContext context, Core.Entities.Exam exam, int examId)
    {
        var completedAttemptIds = await context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == examId && a.Status != AttemptStatus.InProgress)
            .Select(a => new { a.Id, a.QuestionOrder })
            .ToListAsync();

        if (!completedAttemptIds.Any())
            return new List<QuestionAnalyticsResponse>();

        var questionServedCount = new Dictionary<int, int>();
        foreach (var attempt in completedAttemptIds)
        {
            var servedIds = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();
            foreach (var qId in servedIds)
            {
                questionServedCount.TryGetValue(qId, out var current);
                questionServedCount[qId] = current + 1;
            }
        }

        var attemptIds = completedAttemptIds.Select(a => a.Id).ToList();

        var attemptAnswers = await context.AttemptAnswers
            .AsNoTracking()
            .Include(aa => aa.SelectedOptions)
            .Where(aa => attemptIds.Contains(aa.ExamAttemptId))
            .ToListAsync();

        var answersByQuestion = attemptAnswers
            .GroupBy(aa => aa.QuestionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var questionAnalytics = new List<QuestionAnalyticsResponse>();

        foreach (var question in exam.Questions.OrderBy(q => q.Order))
        {
            var served = questionServedCount.GetValueOrDefault(question.Id);
            var answers = answersByQuestion.GetValueOrDefault(question.Id) ?? new();
            var totalAnswered = answers.Count;
            var totalSkipped = served - totalAnswered;

            var correctOptionIds = question.AnswerOptions
                .Where(a => a.IsCorrect)
                .Select(a => a.Id)
                .ToHashSet();

            var correctCount = answers.Count(aa =>
            {
                var selectedIds = aa.SelectedOptions.Select(so => so.AnswerOptionId).ToHashSet();
                return selectedIds.SetEquals(correctOptionIds);
            });

            var incorrectCount = totalAnswered - correctCount;
            var accuracy = totalAnswered > 0
                ? Math.Round((decimal)correctCount / totalAnswered * 100, 2)
                : 0;

            var optionSelectionCounts = new Dictionary<int, int>();
            foreach (var answer in answers)
                foreach (var selected in answer.SelectedOptions)
                {
                    optionSelectionCounts.TryGetValue(selected.AnswerOptionId, out var count);
                    optionSelectionCounts[selected.AnswerOptionId] = count + 1;
                }

            var optionAnalytics = question.AnswerOptions
                .OrderBy(a => a.Order)
                .Select(a =>
                {
                    var selectionCount = optionSelectionCounts.GetValueOrDefault(a.Id);
                    return new OptionAnalyticsResponse
                    {
                        OptionId = a.Id,
                        Text = a.Text,
                        IsCorrect = a.IsCorrect,
                        SelectionCount = selectionCount,
                        SelectionPercentage = totalAnswered > 0
                            ? Math.Round((decimal)selectionCount / totalAnswered * 100, 2)
                            : 0
                    };
                }).ToList();

            questionAnalytics.Add(new QuestionAnalyticsResponse
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Points = question.Points,
                Order = question.Order,
                TotalAnswered = totalAnswered,
                TotalSkipped = Math.Max(0, totalSkipped),
                CorrectCount = correctCount,
                IncorrectCount = incorrectCount,
                AccuracyPercentage = accuracy,
                Options = optionAnalytics
            });
        }

        return questionAnalytics;
    }

    public sealed record AttemptQuestionResults(
        List<QuestionResultResponse> Questions,
        int CorrectCount,
        int WrongCount,
        int Unanswered);

    public static async Task<AttemptQuestionResults> BuildAttemptQuestionResultsAsync(
        ApplicationDbContext context,
        Core.Entities.ExamAttempt attempt,
        List<int> questionOrder)
    {
        var questions = await context.Questions
            .AsNoTracking()
            .Include(q => q.AnswerOptions)
            .Where(q => questionOrder.Contains(q.Id))
            .ToListAsync();

        var questionLookup = questions.ToDictionary(q => q.Id);

        var answerLookup = attempt.AttemptAnswers
            .ToDictionary(
                aa => aa.QuestionId,
                aa => aa.SelectedOptions.Select(so => so.AnswerOptionId).ToHashSet()
            );

        var results = new List<QuestionResultResponse>();
        int correctCount = 0, wrongCount = 0, unanswered = 0;

        foreach (var qId in questionOrder)
        {
            if (!questionLookup.TryGetValue(qId, out var question))
                continue;

            var selectedOptionIds = answerLookup.GetValueOrDefault(qId) ?? new HashSet<int>();
            var isAnswered = selectedOptionIds.Any();

            var correctOptionIds = question.AnswerOptions
                .Where(a => a.IsCorrect)
                .Select(a => a.Id)
                .ToHashSet();

            bool isCorrect;
            decimal earnedPoints;

            if (!isAnswered)
            {
                isCorrect = false;
                earnedPoints = 0;
                unanswered++;
            }
            else
            {
                isCorrect = selectedOptionIds.SetEquals(correctOptionIds);
                earnedPoints = isCorrect ? question.Points : 0;
                if (isCorrect) correctCount++;
                else wrongCount++;
            }

            var optionResults = question.AnswerOptions
                .OrderBy(a => a.Order)
                .Select(a => new OptionResultResponse
                {
                    OptionId = a.Id,
                    Text = a.Text,
                    IsCorrect = a.IsCorrect,
                    WasSelected = selectedOptionIds.Contains(a.Id)
                }).ToList();

            results.Add(new QuestionResultResponse
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Points = question.Points,
                EarnedPoints = earnedPoints,
                IsCorrect = isCorrect,
                IsAnswered = isAnswered,
                Options = optionResults
            });
        }

        return new AttemptQuestionResults(results, correctCount, wrongCount, unanswered);
    }
}

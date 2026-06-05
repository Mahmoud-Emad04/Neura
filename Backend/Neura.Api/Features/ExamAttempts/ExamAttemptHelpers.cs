using Microsoft.AspNetCore.Identity;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Repository.Persistence;
using System.Text.Json;

namespace Neura.Api.Features.ExamAttempts;

internal static class ExamAttemptHelpers
{
    public static async Task<bool> IsEnrolledStudentAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, int courseId, string userId)
    {
        var user = context.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null) return false;

        if (await userManager.IsInRoleAsync(user, DefaultRoles.SuperAdmin) || await userManager.IsInRoleAsync(user, DefaultRoles.Admin))
            return true;

        var courseUser = await context.CourseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId);
        if (courseUser is null)
            return false;

        return ((courseUser.PermissionMask & CoursePermissionMasks.Student) == CoursePermissionMasks.Student);
    }

    public static bool IsTimedOut(DateTime startedAt, int? durationInMinutes)
    {
        if (!durationInMinutes.HasValue)
            return false;

        return DateTime.UtcNow > startedAt.AddMinutes(durationInMinutes.Value);
    }

    public static async Task<decimal> GetAttemptTotalPointsAsync(ApplicationDbContext context, ExamAttempt attempt)
    {
        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();

        return await context.Questions
            .AsNoTracking()
            .Where(q => questionOrder.Contains(q.Id))
            .SumAsync(q => q.Points);
    }

    public static StartAttemptResponse BuildStartAttemptResponse(
        ExamAttempt attempt,
        Exam exam,
        List<Question> servedQuestions,
        Dictionary<int, List<int>> answerOrder,
        Dictionary<int, List<int>>? savedAnswers)
    {
        var optionLookups = servedQuestions
            .SelectMany(q => q.AnswerOptions)
            .ToDictionary(a => a.Id);

        var questionResponses = new List<AttemptQuestionResponse>();
        var order = 1;

        foreach (var question in servedQuestions)
        {
            var orderedOptionIds = answerOrder.GetValueOrDefault(question.Id)
                ?? question.AnswerOptions.OrderBy(a => a.Order).Select(a => a.Id).ToList();

            var options = orderedOptionIds
                .Where(id => optionLookups.ContainsKey(id))
                .Select(id => optionLookups[id])
                .Select(a => new AttemptOptionResponse
                {
                    OptionId = a.Id,
                    Text = a.Text
                }).ToList();

            var saved = savedAnswers?.GetValueOrDefault(question.Id);

            questionResponses.Add(new AttemptQuestionResponse
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Points = question.Points,
                Order = order++,
                Options = options,
                SavedOptionIds = saved
            });
        }

        DateTime? expiresAt = exam.DurationInMinutes.HasValue
            ? attempt.StartedAt.AddMinutes(exam.DurationInMinutes.Value)
            : null;

        return new StartAttemptResponse
        {
            AttemptId = attempt.Id,
            StartedAt = attempt.StartedAt,
            ExpiresAt = expiresAt,
            EnableTabSwitchDetection = exam.EnableTabSwitchDetection,
            MaxViolationsBeforeAutoSubmit = exam.MaxViolationsBeforeAutoSubmit,
            Questions = questionResponses
        };
    }
}

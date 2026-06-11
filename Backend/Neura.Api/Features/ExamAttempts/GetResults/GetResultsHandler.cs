using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.ExamAttempts.GetResults;

internal sealed class GetResultsHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) 
    : IRequestHandler<GetResultsQuery, Result<AttemptResultResponse>>
{
    public async Task<Result<AttemptResultResponse>> Handle(
        GetResultsQuery query, CancellationToken ct)
    {
        var attemptId = query.AttemptId;
        var userId = query.UserId;

        var attempt = await context.ExamAttempts
            .AsNoTracking()
            .Include(a => a.Exam)
                .ThenInclude(e => e.Lesson)
                    .ThenInclude(l => l.Section)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedOptions)
            .Include(a => a.Violations)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt is null)
            return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.AttemptNotFound);

        // Allow access if the user is the attempt owner
        if (attempt.UserId != userId)
        {
            // Check if user has a privileged role (Admin / SuperAdmin / CourseOwner / CoInstructor)
            var hasAccess = await HasPrivilegedAccessAsync(userId, attempt, ct);
            if (!hasAccess)
                return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.NotAttemptOwner);
        }

        if (attempt.Status == AttemptStatus.InProgress)
            return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.ResultsNotAvailable);

        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();

        var questions = await context.Questions
            .AsNoTracking()
            .Include(q => q.AnswerOptions)
            .Where(q => questionOrder.Contains(q.Id))
            .ToListAsync(ct);

        var questionLookup = questions.ToDictionary(q => q.Id);

        var answerLookup = attempt.AttemptAnswers
            .ToDictionary(
                aa => aa.QuestionId,
                aa => aa.SelectedOptions.Select(so => so.AnswerOptionId).ToHashSet()
            );

        var questionResults = new List<QuestionResultResponse>();
        int correctCount = 0;
        int wrongCount = 0;
        int unanswered = 0;

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

                if (isCorrect)
                    correctCount++;
                else
                    wrongCount++;
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

            questionResults.Add(new QuestionResultResponse
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

        var totalPoints = questionResults.Sum(q => q.Points);

        var response = new AttemptResultResponse
        {
            AttemptId = attempt.Id,
            Score = attempt.Score ?? 0,
            ScorePercentage = attempt.ScorePercentage ?? 0,
            TotalPoints = totalPoints,
            PassingScorePercentage = attempt.Exam.PassingScorePercentage,
            Passed = attempt.Passed ?? false,
            Status = attempt.Status.ToString(),
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt ?? attempt.StartedAt,
            TotalQuestions = questionOrder.Count,
            CorrectAnswers = correctCount,
            WrongAnswers = wrongCount,
            Unanswered = unanswered,
            ViolationCount = attempt.Violations.Count,
            Questions = questionResults
        };

        return Result.Success(response);
    }

    /// <summary>
    ///     Checks whether the user has Admin, SuperAdmin, CourseOwner, or CoInstructor access.
    /// </summary>
    private async Task<bool> HasPrivilegedAccessAsync(
        string userId, ExamAttempt attempt, CancellationToken ct)
    {
        // 1. Check global roles: Admin / SuperAdmin
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return false;

        if (await userManager.IsInRoleAsync(user, DefaultRoles.SuperAdmin) ||
            await userManager.IsInRoleAsync(user, DefaultRoles.Admin))
            return true;

        // 2. Check course-level roles: CourseOwner (Level 4) or CoInstructor (Level 3)
        var courseId = attempt.Exam.Lesson.Section.CourseId;

        var courseUser = await context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId, ct);

        return courseUser is not null &&
               courseUser.CourseRole.Level >= (int)CourseRoleType.CoInstructor;
    }
}

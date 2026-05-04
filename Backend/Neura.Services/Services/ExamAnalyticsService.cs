using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using System.Text.Json;

namespace Neura.Services.Services;

public class ExamAnalyticsService : IExamAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public ExamAnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ══════════════════════════════════════════
    //  EXAM ANALYTICS (Full Dashboard)
    // ══════════════════════════════════════════
    public async Task<Result<ExamAnalyticsResponse>> GetExamAnalyticsAsync(int examId, string userId)
    {
        var exam = await _context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .Include(e => e.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.LessonId == examId);

        if (exam is null)
            return Result.Failure<ExamAnalyticsResponse>(AnalyticsErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await HasInstructorPermissionAsync(courseId, userId))
            return Result.Failure<ExamAnalyticsResponse>(AnalyticsErrors.Forbidden);

        // ── Load all completed attempts (exclude InProgress) ──
        var completedStatuses = new[]
        {
            AttemptStatus.Submitted,
            AttemptStatus.TimedOut,
            AttemptStatus.AutoSubmitted,
            AttemptStatus.Graded
        };

        var allAttempts = await _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == exam.Id)
            .ToListAsync();

        var completedAttempts = allAttempts
            .Where(a => completedStatuses.Contains(a.Status))
            .ToList();

        var inProgressCount = allAttempts.Count(a => a.Status == AttemptStatus.InProgress);

        // ── Overview Stats ──
        var totalAttempts = allAttempts.Count;
        var uniqueStudents = allAttempts.Select(a => a.UserId).Distinct().Count();

        // ── Score Stats (from completed only) ──
        decimal avgScore = 0, avgPercentage = 0, highest = 0, lowest = 0, median = 0;
        int passedCount = 0, failedCount = 0;

        if (completedAttempts.Any())
        {
            var percentages = completedAttempts
                .Where(a => a.ScorePercentage.HasValue)
                .Select(a => a.ScorePercentage!.Value)
                .OrderBy(p => p)
                .ToList();

            if (percentages.Any())
            {
                avgScore = Math.Round(completedAttempts.Average(a => a.Score ?? 0), 2);
                avgPercentage = Math.Round(percentages.Average(), 2);
                highest = percentages.Last();
                lowest = percentages.First();
                median = CalculateMedian(percentages);
            }

            passedCount = completedAttempts.Count(a => a.Passed == true);
            failedCount = completedAttempts.Count(a => a.Passed == false);
        }

        var passRate = completedAttempts.Any()
            ? Math.Round((decimal)passedCount / completedAttempts.Count * 100, 2)
            : 0;

        // ── Violation Stats ──
        var violationStats = await _context.AttemptViolations
            .AsNoTracking()
            .Where(v => v.ExamAttempt.ExamId == exam.Id)
            .GroupBy(v => v.ExamAttemptId)
            .Select(g => new { AttemptId = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalViolations = violationStats.Sum(v => v.Count);
        var studentsWithViolations = violationStats.Count;

        // ── Per-Question Analytics ──
        var questionAnalytics = await BuildQuestionAnalyticsAsync(exam, exam.Id);

        var response = new ExamAnalyticsResponse
        {
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            TotalAttempts = totalAttempts,
            UniqueStudents = uniqueStudents,
            CompletedAttempts = completedAttempts.Count,
            InProgressAttempts = inProgressCount,
            TimedOutAttempts = completedAttempts.Count(a => a.Status == AttemptStatus.TimedOut),
            AutoSubmittedAttempts = completedAttempts.Count(a => a.Status == AttemptStatus.AutoSubmitted),
            AverageScore = avgScore,
            AverageScorePercentage = avgPercentage,
            HighestScorePercentage = highest,
            LowestScorePercentage = lowest,
            MedianScorePercentage = median,
            PassedCount = passedCount,
            FailedCount = failedCount,
            PassRate = passRate,
            TotalViolations = totalViolations,
            StudentsWithViolations = studentsWithViolations,
            Questions = questionAnalytics
        };

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  STUDENT ATTEMPTS LIST (Paginated)
    // ══════════════════════════════════════════
    public async Task<Result<ExamStudentAttemptsResponse>> GetStudentAttemptsAsync(
        int examId, string userId, int page = 1, int pageSize = 20, string? sortBy = null, bool descending = true)
    {
        var exam = await _context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == examId);

        if (exam is null)
            return Result.Failure<ExamStudentAttemptsResponse>(AnalyticsErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await HasInstructorPermissionAsync(courseId, userId))
            return Result.Failure<ExamStudentAttemptsResponse>(AnalyticsErrors.Forbidden);

        // ── Base query ──
        var query = _context.ExamAttempts
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Violations)
            .Where(a => a.ExamId == exam.Id && a.Status != AttemptStatus.InProgress);

        // ── Sorting ──
        query = sortBy?.ToLower() switch
        {
            "score" => descending
                ? query.OrderByDescending(a => a.ScorePercentage)
                : query.OrderBy(a => a.ScorePercentage),
            "name" => descending
                ? query.OrderByDescending(a => $"{a.User.FirstName} {a.User.LastName}")
                : query.OrderBy(a => $"{a.User.FirstName} {a.User.LastName}"),
            "duration" => descending
                ? query.OrderByDescending(a =>
                    a.SubmittedAt.HasValue
                        ? EF.Functions.DateDiffSecond(a.StartedAt, a.SubmittedAt.Value)
                        : 0)
                : query.OrderBy(a =>
                    a.SubmittedAt.HasValue
                        ? EF.Functions.DateDiffSecond(a.StartedAt, a.SubmittedAt.Value)
                        : 0),
            "violations" => descending
                ? query.OrderByDescending(a => a.Violations.Count)
                : query.OrderBy(a => a.Violations.Count),
            _ => descending
                ? query.OrderByDescending(a => a.SubmittedAt)
                : query.OrderBy(a => a.SubmittedAt)
        };

        // ── Pagination ──
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var attempts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ── Calculate total points per attempt ──
        var attemptTotalPoints = new Dictionary<int, decimal>();
        foreach (var attempt in attempts)
        {
            var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();
            var points = await _context.Questions
                .AsNoTracking()
                .Where(q => questionOrder.Contains(q.Id))
                .SumAsync(q => q.Points);
            attemptTotalPoints[attempt.Id] = points;
        }

        var attemptResponses = attempts.Select(a => new StudentAttemptSummaryResponse
        {
            AttemptId = a.Id,
            UserId = a.UserId,
            StudentName = a.User.FirstName ?? a.User.UserName ?? $"{a.User.FirstName} {a.User.LastName}",
            StudentEmail = a.User.Email,
            Score = a.Score ?? 0,
            ScorePercentage = a.ScorePercentage ?? 0,
            TotalPoints = attemptTotalPoints.GetValueOrDefault(a.Id),
            Passed = a.Passed ?? false,
            Status = a.Status.ToString(),
            StartedAt = a.StartedAt,
            SubmittedAt = a.SubmittedAt,
            DurationInSeconds = a.SubmittedAt.HasValue
                ? (int)(a.SubmittedAt.Value - a.StartedAt).TotalSeconds
                : null,
            ViolationCount = a.Violations.Count
        }).ToList();

        var response = new ExamStudentAttemptsResponse
        {
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Attempts = attemptResponses
        };

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  STUDENT ATTEMPT DETAIL (Instructor views a student's answers)
    // ══════════════════════════════════════════
    public async Task<Result<AttemptResultResponse>> GetStudentAttemptDetailAsync(
        int attemptId, string userId)
    {
        var attempt = await _context.ExamAttempts
            .AsNoTracking()
            .Include(a => a.Exam)
                .ThenInclude(e => e.Lesson)
                    .ThenInclude(l => l.Section)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedOptions)
            .Include(a => a.Violations)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null)
            return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.AttemptNotFound);

        // Instructor auth — NOT student auth
        var courseId = attempt.Exam.Lesson.Section.CourseId;
        if (!await HasInstructorPermissionAsync(courseId, userId))
            return Result.Failure<AttemptResultResponse>(AnalyticsErrors.Forbidden);

        if (attempt.Status == AttemptStatus.InProgress)
            return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.ResultsNotAvailable);

        // Reuse the same result-building logic
        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();

        var questions = await _context.Questions
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

        var questionResults = new List<QuestionResultResponse>();
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

    // ══════════════════════════════════════════
    //  SCORE DISTRIBUTION
    // ══════════════════════════════════════════
    public async Task<Result<ScoreDistributionResponse>> GetScoreDistributionAsync(int examId, string userId)
    {
        var exam = await _context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == examId);

        if (exam is null)
            return Result.Failure<ScoreDistributionResponse>(AnalyticsErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await HasInstructorPermissionAsync(courseId, userId))
            return Result.Failure<ScoreDistributionResponse>(AnalyticsErrors.Forbidden);

        var percentages = await _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == exam.Id
                     && a.Status != AttemptStatus.InProgress
                     && a.ScorePercentage.HasValue)
            .Select(a => a.ScorePercentage!.Value)
            .ToListAsync();

        if (!percentages.Any())
            return Result.Failure<ScoreDistributionResponse>(AnalyticsErrors.NoAttempts);

        var totalCount = percentages.Count;

        // Build 10 buckets: 0-10, 11-20, ..., 91-100
        var buckets = new List<ScoreBucket>();

        for (int i = 0; i < 10; i++)
        {
            var lower = i * 10;
            var upper = (i + 1) * 10;
            var label = i == 0 ? $"0-{upper}" : $"{lower + 1}-{upper}";

            var count = percentages.Count(p =>
                i == 0
                    ? p >= lower && p <= upper
                    : p > lower && p <= upper);

            buckets.Add(new ScoreBucket
            {
                Range = label,
                Count = count,
                Percentage = Math.Round((decimal)count / totalCount * 100, 2)
            });
        }

        return Result.Success(new ScoreDistributionResponse { Buckets = buckets });
    }

    // ══════════════════════════════════════════
    //  PRIVATE — Per-Question Analytics Builder
    // ══════════════════════════════════════════
    private async Task<List<QuestionAnalyticsResponse>> BuildQuestionAnalyticsAsync(
        Exam exam, int examId)
    {
        // Get all completed attempt IDs for this exam
        var completedAttemptIds = await _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == examId && a.Status != AttemptStatus.InProgress)
            .Select(a => new { a.Id, a.QuestionOrder })
            .ToListAsync();

        if (!completedAttemptIds.Any())
            return new List<QuestionAnalyticsResponse>();

        // Count how many times each question was served
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

        // Get all attempt answers for this exam's attempts
        var attemptIds = completedAttemptIds.Select(a => a.Id).ToList();

        var attemptAnswers = await _context.AttemptAnswers
            .AsNoTracking()
            .Include(aa => aa.SelectedOptions)
            .Where(aa => attemptIds.Contains(aa.ExamAttemptId))
            .ToListAsync();

        // Group answers by question
        var answersByQuestion = attemptAnswers
            .GroupBy(aa => aa.QuestionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build analytics per question
        var questionAnalytics = new List<QuestionAnalyticsResponse>();

        foreach (var question in exam.Questions.OrderBy(q => q.Order))
        {
            var served = questionServedCount.GetValueOrDefault(question.Id);
            var answers = answersByQuestion.GetValueOrDefault(question.Id) ?? new();
            var totalAnswered = answers.Count;
            var totalSkipped = served - totalAnswered;

            // Calculate correct count
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

            // Per-option selection distribution
            var optionSelectionCounts = new Dictionary<int, int>();
            foreach (var answer in answers)
            {
                foreach (var selected in answer.SelectedOptions)
                {
                    optionSelectionCounts.TryGetValue(selected.AnswerOptionId, out var count);
                    optionSelectionCounts[selected.AnswerOptionId] = count + 1;
                }
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

    // ══════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════
    private async Task<bool> HasInstructorPermissionAsync(int courseId, string userId)
    {
        var courseUser = await _context.CourseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId);

        if (courseUser is null)
            return false;

        return ((courseUser.PermissionMask & CoursePermissionMasks.CoInstructor) == CoursePermissionMasks.CoInstructor)
            || ((courseUser.PermissionMask & CoursePermissionMasks.CourseOwner) == CoursePermissionMasks.CourseOwner);
    }

    private static decimal CalculateMedian(List<decimal> sorted)
    {
        if (!sorted.Any()) return 0;

        var count = sorted.Count;
        if (count % 2 == 0)
            return Math.Round((sorted[count / 2 - 1] + sorted[count / 2]) / 2, 2);

        return sorted[count / 2];
    }
}
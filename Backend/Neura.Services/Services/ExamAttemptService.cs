using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.ExamAttempt;
using Neura.Core.Enums;
using System.Text.Json;

namespace Neura.Services.Services;

public class ExamAttemptService : IExamAttemptService
{
    private readonly ApplicationDbContext _context;
    private readonly IGradingService _gradingService;

    public ExamAttemptService(ApplicationDbContext context, IGradingService gradingService)
    {
        _context = context;
        _gradingService = gradingService;
    }

    // ══════════════════════════════════════════
    //  GET EXAM INFO — NO CHANGES
    // ══════════════════════════════════════════
    public async Task<Result<ExamInfoResponse>> GetExamInfoAsync(int lessonId, string userId)
    {
        // ... exactly the same as Step 4 — no changes ...
        var exam = await _context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId);

        if (exam is null)
            return Result.Failure<ExamInfoResponse>(ExamAttemptErrors.ExamNotFound);

        if (!exam.IsPublished)
            return Result.Failure<ExamInfoResponse>(ExamAttemptErrors.ExamNotPublished);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await IsEnrolledStudentAsync(courseId, userId))
            return Result.Failure<ExamInfoResponse>(ExamAttemptErrors.NotEnrolled);

        var attemptsTaken = await _context.ExamAttempts
            .AsNoTracking()
            .CountAsync(a => a.ExamId == exam.Id && a.UserId == userId);

        var inProgressAttempt = await _context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == exam.Id
                     && a.UserId == userId
                     && a.Status == AttemptStatus.InProgress)
            .Select(a => new { a.Id, a.StartedAt })
            .FirstOrDefaultAsync();

        bool hasInProgressAttempt = false;
        int? inProgressAttemptId = null;

        if (inProgressAttempt is not null)
        {
            if (IsTimedOut(inProgressAttempt.StartedAt, exam.DurationInMinutes))
            {
                hasInProgressAttempt = false;
            }
            else
            {
                hasInProgressAttempt = true;
                inProgressAttemptId = inProgressAttempt.Id;
            }
        }

        var questionCount = exam.NumberOfQuestionsToServe ?? exam.Questions.Count;

        var totalPoints = exam.NumberOfQuestionsToServe.HasValue
            ? exam.Questions.OrderBy(q => q.Points).Take(questionCount).Sum(q => q.Points)
            : exam.Questions.Sum(q => q.Points);

        int? remainingAttempts = exam.MaxAttempts.HasValue
            ? Math.Max(0, exam.MaxAttempts.Value - attemptsTaken)
            : null;

        var response = new ExamInfoResponse
        {
            ExamId = lessonId,
            Title = exam.Title,
            Description = exam.Description,
            DurationInMinutes = exam.DurationInMinutes,
            QuestionCount = questionCount,
            TotalPoints = totalPoints,
            PassingScorePercentage = exam.PassingScorePercentage,
            MaxAttempts = exam.MaxAttempts,
            AttemptsTaken = attemptsTaken,
            RemainingAttempts = remainingAttempts,
            EnableTabSwitchDetection = exam.EnableTabSwitchDetection,
            MaxViolationsBeforeAutoSubmit = exam.MaxViolationsBeforeAutoSubmit,
            HasInProgressAttempt = hasInProgressAttempt,
            InProgressAttemptId = inProgressAttemptId
        };

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  START ATTEMPT — NO CHANGES
    // ══════════════════════════════════════════
    public async Task<Result<StartAttemptResponse>> StartAttemptAsync(int lessonId, string userId)
    {
        // ... exactly the same as Step 4 — no changes ...
        var exam = await _context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .Include(e => e.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId);

        if (exam is null)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.ExamNotFound);

        if (!exam.IsPublished)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.ExamNotPublished);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await IsEnrolledStudentAsync(courseId, userId))
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.NotEnrolled);

        var existingInProgress = await _context.ExamAttempts
            .AnyAsync(a => a.ExamId == exam.Id
                        && a.UserId == userId
                        && a.Status == AttemptStatus.InProgress);

        if (existingInProgress)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.AttemptAlreadyInProgress);

        if (exam.MaxAttempts.HasValue)
        {
            var attemptsTaken = await _context.ExamAttempts
                .AsNoTracking()
                .CountAsync(a => a.ExamId == exam.Id && a.UserId == userId);

            if (attemptsTaken >= exam.MaxAttempts.Value)
                return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.MaxAttemptsReached);
        }

        var allQuestions = exam.Questions.ToList();

        List<Question> servedQuestions;
        if (exam.NumberOfQuestionsToServe.HasValue
            && exam.NumberOfQuestionsToServe.Value < allQuestions.Count)
        {
            servedQuestions = allQuestions
                .OrderBy(_ => Guid.NewGuid())
                .Take(exam.NumberOfQuestionsToServe.Value)
                .ToList();
        }
        else
        {
            servedQuestions = allQuestions;
        }

        if (exam.ShuffleQuestions)
            servedQuestions = servedQuestions.OrderBy(_ => Guid.NewGuid()).ToList();
        else
            servedQuestions = servedQuestions.OrderBy(q => q.Order).ToList();

        var answerOrder = new Dictionary<int, List<int>>();
        foreach (var question in servedQuestions)
        {
            var optionIds = exam.ShuffleAnswers
                ? question.AnswerOptions.OrderBy(_ => Guid.NewGuid()).Select(a => a.Id).ToList()
                : question.AnswerOptions.OrderBy(a => a.Order).Select(a => a.Id).ToList();

            answerOrder[question.Id] = optionIds;
        }

        var questionOrderJson = JsonSerializer.Serialize(servedQuestions.Select(q => q.Id).ToList());
        var answerOrderJson = JsonSerializer.Serialize(answerOrder);

        var attempt = new ExamAttempt
        {
            ExamId = exam.Id,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            Status = AttemptStatus.InProgress,
            QuestionOrder = questionOrderJson,
            AnswerOrder = answerOrderJson
        };

        _context.ExamAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        var response = BuildStartAttemptResponse(attempt, exam, servedQuestions, answerOrder, null);

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  RESUME ATTEMPT — UPDATED to use IGradingService
    // ══════════════════════════════════════════
    public async Task<Result<StartAttemptResponse>> ResumeAttemptAsync(int attemptId, string userId)
    {
        var attempt = await _context.ExamAttempts
            .Include(a => a.Exam)
                .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.AnswerOptions)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.AttemptNotFound);

        if (attempt.UserId != userId)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.NotAttemptOwner);

        if (attempt.Status != AttemptStatus.InProgress)
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.AttemptNotInProgress);

        if (IsTimedOut(attempt.StartedAt, attempt.Exam.DurationInMinutes))
        {
            await _gradingService.GradeAttemptAsync(attempt, AttemptStatus.TimedOut);
            return Result.Failure<StartAttemptResponse>(ExamAttemptErrors.AttemptTimedOut);
        }

        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();
        var answerOrder = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(attempt.AnswerOrder) ?? new();

        var allQuestions = attempt.Exam.Questions.ToDictionary(q => q.Id);
        var servedQuestions = questionOrder
            .Where(id => allQuestions.ContainsKey(id))
            .Select(id => allQuestions[id])
            .ToList();

        var savedAnswers = attempt.AttemptAnswers
            .ToDictionary(
                aa => aa.QuestionId,
                aa => aa.SelectedOptions.Select(so => so.AnswerOptionId).ToList()
            );

        var response = BuildStartAttemptResponse(attempt, attempt.Exam, servedQuestions, answerOrder, savedAnswers);

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  SAVE ANSWER — UPDATED to use IGradingService
    // ══════════════════════════════════════════
    public async Task<Result> SaveAnswerAsync(
        int attemptId, int questionId, SaveAnswerRequest request, string userId)
    {
        var attempt = await _context.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.AttemptAnswers.Where(aa => aa.QuestionId == questionId))
                .ThenInclude(aa => aa.SelectedOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null)
            return Result.Failure(ExamAttemptErrors.AttemptNotFound);

        if (attempt.UserId != userId)
            return Result.Failure(ExamAttemptErrors.NotAttemptOwner);

        if (attempt.Status != AttemptStatus.InProgress)
            return Result.Failure(ExamAttemptErrors.AttemptNotInProgress);

        if (IsTimedOut(attempt.StartedAt, attempt.Exam.DurationInMinutes))
        {
            await _gradingService.GradeAttemptAsync(attempt, AttemptStatus.TimedOut);
            return Result.Failure(ExamAttemptErrors.AttemptTimedOut);
        }

        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();
        if (!questionOrder.Contains(questionId))
            return Result.Failure(ExamAttemptErrors.QuestionNotInAttempt);

        if (request.SelectedOptionIds.Any())
        {
            var validOptionIds = await _context.AnswerOptions
                .AsNoTracking()
                .Where(ao => ao.QuestionId == questionId)
                .Select(ao => ao.Id)
                .ToHashSetAsync();

            var invalidOptions = request.SelectedOptionIds
                .Where(id => !validOptionIds.Contains(id))
                .ToList();

            if (invalidOptions.Any())
                return Result.Failure(ExamAttemptErrors.InvalidSelectedOptions);
        }

        var question = await _context.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question is not null
            && question.QuestionType is QuestionType.SingleChoice or QuestionType.TrueFalse
            && request.SelectedOptionIds.Count > 1)
        {
            return Result.Failure(ExamAttemptErrors.SingleChoiceMultipleSelections);
        }

        var existingAnswer = attempt.AttemptAnswers.FirstOrDefault();

        if (existingAnswer is not null)
        {
            _context.AttemptAnswerOptions.RemoveRange(existingAnswer.SelectedOptions);

            if (request.SelectedOptionIds.Any())
            {
                foreach (var optionId in request.SelectedOptionIds)
                {
                    existingAnswer.SelectedOptions.Add(new AttemptAnswerOption
                    {
                        AttemptAnswerId = existingAnswer.Id,
                        AnswerOptionId = optionId
                    });
                }
            }
            else
            {
                _context.AttemptAnswers.Remove(existingAnswer);
            }
        }
        else if (request.SelectedOptionIds.Any())
        {
            var newAnswer = new AttemptAnswer
            {
                ExamAttemptId = attemptId,
                QuestionId = questionId,
                SelectedOptions = request.SelectedOptionIds
                    .Select(optionId => new AttemptAnswerOption
                    {
                        AnswerOptionId = optionId
                    }).ToList()
            };

            _context.AttemptAnswers.Add(newAnswer);
        }

        await _context.SaveChangesAsync();
        return Result.Success();
    }

    // ══════════════════════════════════════════
    //  SUBMIT — UPDATED to use IGradingService
    // ══════════════════════════════════════════
    public async Task<Result<SubmitAttemptResponse>> SubmitAsync(int attemptId, string userId)
    {
        var attempt = await _context.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null)
            return Result.Failure<SubmitAttemptResponse>(ExamAttemptErrors.AttemptNotFound);

        if (attempt.UserId != userId)
            return Result.Failure<SubmitAttemptResponse>(ExamAttemptErrors.NotAttemptOwner);

        if (attempt.Status != AttemptStatus.InProgress)
            return Result.Failure<SubmitAttemptResponse>(ExamAttemptErrors.AttemptNotInProgress);

        var status = IsTimedOut(attempt.StartedAt, attempt.Exam.DurationInMinutes)
            ? AttemptStatus.TimedOut
            : AttemptStatus.Submitted;

        await _gradingService.GradeAttemptAsync(attempt, status);

        var response = new SubmitAttemptResponse
        {
            AttemptId = attempt.Id,
            Score = attempt.Score!.Value,
            ScorePercentage = attempt.ScorePercentage!.Value,
            TotalPoints = await GetAttemptTotalPointsAsync(attempt),
            PassingScorePercentage = attempt.Exam.PassingScorePercentage,
            Passed = attempt.Passed!.Value,
            Status = attempt.Status.ToString(),
            StartedAt = attempt.StartedAt,
            SubmittedAt = attempt.SubmittedAt!.Value
        };

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  GET RESULTS — NO CHANGES
    // ══════════════════════════════════════════
    public async Task<Result<AttemptResultResponse>> GetResultsAsync(int attemptId, string userId)
    {
        // ... exactly the same as Step 4 — no changes ...
        var attempt = await _context.ExamAttempts
            .AsNoTracking()
            .Include(a => a.Exam)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedOptions)
            .Include(a => a.Violations)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null)
            return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.AttemptNotFound);

        if (attempt.UserId != userId)
            return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.NotAttemptOwner);

        if (attempt.Status == AttemptStatus.InProgress)
            return Result.Failure<AttemptResultResponse>(ExamAttemptErrors.ResultsNotAvailable);

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

    // ══════════════════════════════════════════
    //  RECORD VIOLATION — UPDATED to use IGradingService
    // ══════════════════════════════════════════
    public async Task<Result<ViolationResponse>> RecordViolationAsync(
        int attemptId, ViolationRequest request, string userId)
    {
        var attempt = await _context.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Violations)
            .Include(a => a.AttemptAnswers)
                .ThenInclude(aa => aa.SelectedOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null)
            return Result.Failure<ViolationResponse>(ExamAttemptErrors.AttemptNotFound);

        if (attempt.UserId != userId)
            return Result.Failure<ViolationResponse>(ExamAttemptErrors.NotAttemptOwner);

        if (attempt.Status != AttemptStatus.InProgress)
            return Result.Failure<ViolationResponse>(ExamAttemptErrors.AttemptNotInProgress);

        var violation = new AttemptViolation
        {
            ExamAttemptId = attemptId,
            ViolationType = request.ViolationType,
            OccurredAt = request.OccurredAt
        };

        _context.AttemptViolations.Add(violation);

        var totalViolations = attempt.Violations.Count + 1;
        var autoSubmitted = false;

        if (attempt.Exam.EnableTabSwitchDetection
            && attempt.Exam.MaxViolationsBeforeAutoSubmit.HasValue
            && totalViolations >= attempt.Exam.MaxViolationsBeforeAutoSubmit.Value)
        {
            await _gradingService.GradeAttemptAsync(attempt, AttemptStatus.AutoSubmitted);
            autoSubmitted = true;
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        var response = new ViolationResponse
        {
            TotalViolations = totalViolations,
            MaxViolationsBeforeAutoSubmit = attempt.Exam.MaxViolationsBeforeAutoSubmit,
            AttemptAutoSubmitted = autoSubmitted
        };

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  PRIVATE HELPERS — NO CHANGES
    // ══════════════════════════════════════════
    private async Task<bool> IsEnrolledStudentAsync(int courseId, string userId)
    {
        var courseUser = await _context.CourseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId);

        if (courseUser is null)
            return false;

        return ((courseUser.PermissionMask & CoursePermissionMasks.Student) == CoursePermissionMasks.Student);
    }

    private static bool IsTimedOut(DateTime startedAt, int? durationInMinutes)
    {
        if (!durationInMinutes.HasValue)
            return false;

        return DateTime.UtcNow > startedAt.AddMinutes(durationInMinutes.Value);
    }

    private async Task<decimal> GetAttemptTotalPointsAsync(ExamAttempt attempt)
    {
        var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();

        return await _context.Questions
            .AsNoTracking()
            .Where(q => questionOrder.Contains(q.Id))
            .SumAsync(q => q.Points);
    }

    private static StartAttemptResponse BuildStartAttemptResponse(
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
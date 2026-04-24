using Ganss.Xss;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Question;

namespace Neura.Services.Services;

public class QuestionService : IQuestionService
{
    private readonly ApplicationDbContext _context;
    private readonly HtmlSanitizer _sanitizer;

    public QuestionService(ApplicationDbContext context, HtmlSanitizer sanitizer)
    {
        _context = context;
        _sanitizer = sanitizer;
    }

    // ══════════════════════════════════════════
    //  ADD QUESTION
    // ══════════════════════════════════════════
    public async Task<Result<QuestionResponse>> AddAsync(
        int examId, CreateQuestionRequest request, string userId)
    {
        var exam = await _context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam is null)
            return Result.Failure<QuestionResponse>(ExamErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await HasInstructorPermissionAsync(courseId, userId))
            return Result.Failure<QuestionResponse>(QuestionErrors.Forbidden);

        var maxOrder = await _context.Questions
            .AsNoTracking()
            .Where(q => q.ExamId == examId)
            .MaxAsync(q => (int?)q.Order) ?? 0;

        var question = new Question
        {
            ExamId = examId,
            QuestionText = _sanitizer.Sanitize(request.QuestionText),
            QuestionType = request.QuestionType,
            Points = request.Points,
            Order = maxOrder + 1,
            AnswerOptions = request.Options.Select((opt, index) => new AnswerOption
            {
                Text = _sanitizer.Sanitize(opt.Text),
                IsCorrect = opt.IsCorrect,
                Order = index + 1
            }).ToList()
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        var response = question.Adapt<QuestionResponse>();
        response.HasAttempts = false;

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  UPDATE QUESTION
    // ══════════════════════════════════════════
    public async Task<Result<QuestionResponse>> UpdateAsync(
        int questionId, UpdateQuestionRequest request, string userId)
    {
        var question = await _context.Questions
            .Include(q => q.AnswerOptions)
            .Include(q => q.Exam)
                .ThenInclude(e => e.Lesson)
                    .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question is null)
            return Result.Failure<QuestionResponse>(QuestionErrors.QuestionNotFound);

        var courseId = question.Exam.Lesson.Section.CourseId;
        if (!await HasInstructorPermissionAsync(courseId, userId))
            return Result.Failure<QuestionResponse>(QuestionErrors.Forbidden);

        var hasAttempts = await _context.AttemptAnswers
            .AnyAsync(aa => aa.QuestionId == questionId);

        if (hasAttempts)
        {
            var restrictedResult = await ApplyRestrictedUpdateAsync(question, request);
            if (restrictedResult.IsFailure)
                return Result.Failure<QuestionResponse>(restrictedResult.Error);
        }
        else
        {
            ApplyFullUpdate(question, request);
        }

        question.Exam.UpdatedOn = DateTime.UtcNow;
        question.Exam.UpdatedById = userId;
        await _context.SaveChangesAsync();

        var response = question.Adapt<QuestionResponse>();
        response.HasAttempts = hasAttempts;

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  DELETE QUESTION
    // ══════════════════════════════════════════
    public async Task<Result> DeleteAsync(int questionId, string userId)
    {
        var question = await _context.Questions
            .Include(q => q.Exam)
                .ThenInclude(e => e.Lesson)
                    .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question is null)
            return Result.Failure(QuestionErrors.QuestionNotFound);

        var courseId = question.Exam.Lesson.Section.CourseId;
        if (!await HasInstructorPermissionAsync(courseId, userId))
            return Result.Failure(QuestionErrors.Forbidden);

        var hasAttempts = await _context.AttemptAnswers
            .AnyAsync(aa => aa.QuestionId == questionId);

        if (hasAttempts)
            return Result.Failure(QuestionErrors.CannotDeleteWithAttempts);

        _context.Questions.Remove(question);

        var remainingQuestions = await _context.Questions
            .Where(q => q.ExamId == question.ExamId && q.Id != questionId)
            .OrderBy(q => q.Order)
            .ToListAsync();

        for (int i = 0; i < remainingQuestions.Count; i++)
            remainingQuestions[i].Order = i + 1;

        question.Exam.UpdatedOn = DateTime.UtcNow;
        question.Exam.UpdatedById = userId;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    // ══════════════════════════════════════════
    //  REORDER QUESTIONS
    // ══════════════════════════════════════════
    public async Task<Result> ReorderAsync(
        int examId, ReorderQuestionsRequest request, string userId)
    {
        var exam = await _context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam is null)
            return Result.Failure(ExamErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        if (!await HasInstructorPermissionAsync(courseId, userId))
            return Result.Failure(QuestionErrors.Forbidden);

        var questions = await _context.Questions
            .Where(q => q.ExamId == examId)
            .ToListAsync();

        var existingIds = questions.Select(q => q.Id).ToHashSet();
        var requestIds = request.OrderedQuestionIds.ToHashSet();

        if (!existingIds.SetEquals(requestIds))
            return Result.Failure(QuestionErrors.ReorderIdsMismatch);

        if (request.OrderedQuestionIds.Count != requestIds.Count)
            return Result.Failure(QuestionErrors.ReorderDuplicateIds);

        var questionLookup = questions.ToDictionary(q => q.Id);
        for (int i = 0; i < request.OrderedQuestionIds.Count; i++)
            questionLookup[request.OrderedQuestionIds[i]].Order = i + 1;

        await _context.SaveChangesAsync();
        return Result.Success();
    }

    // ══════════════════════════════════════════
    //  PRIVATE — RESTRICTED UPDATE (has attempts)
    // ══════════════════════════════════════════
    private async Task<Result> ApplyRestrictedUpdateAsync(
        Question question, UpdateQuestionRequest request)
    {
        if (request.QuestionType != question.QuestionType)
            return Result.Failure(QuestionErrors.CannotChangeQuestionType);

        var existingCorrectIds = question.AnswerOptions
            .Where(a => a.IsCorrect)
            .Select(a => a.Id)
            .ToHashSet();

        var requestCorrectIds = request.Options
            .Where(o => o.Id.HasValue && o.IsCorrect)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        if (!existingCorrectIds.SetEquals(requestCorrectIds))
            return Result.Failure(QuestionErrors.CannotChangeCorrectAnswers);

        var existingOptionIds = question.AnswerOptions.Select(a => a.Id).ToHashSet();
        var requestOptionIds = request.Options
            .Where(o => o.Id.HasValue)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        var removedOptionIds = existingOptionIds.Except(requestOptionIds).ToList();

        if (removedOptionIds.Any())
        {
            var removedOptionHasSelections = await _context.AttemptAnswerOptions
                .AnyAsync(aao => removedOptionIds.Contains(aao.AnswerOptionId));

            if (removedOptionHasSelections)
                return Result.Failure(QuestionErrors.CannotRemoveSelectedOptions);
        }

        if (request.Options.Any(o => !o.Id.HasValue))
            return Result.Failure(QuestionErrors.CannotAddOptionsAfterAttempts);

        // Allowed: cosmetic text + points
        question.QuestionText = _sanitizer.Sanitize(request.QuestionText);
        question.Points = request.Points;

        foreach (var optionRequest in request.Options.Where(o => o.Id.HasValue))
        {
            var existing = question.AnswerOptions
                .FirstOrDefault(a => a.Id == optionRequest.Id!.Value);

            if (existing is not null)
                existing.Text = _sanitizer.Sanitize(optionRequest.Text);
        }

        return Result.Success();
    }

    // ══════════════════════════════════════════
    //  PRIVATE — FULL UPDATE (no attempts)
    // ══════════════════════════════════════════
    private void ApplyFullUpdate(Question question, UpdateQuestionRequest request)
    {
        question.QuestionText = _sanitizer.Sanitize(request.QuestionText);
        question.QuestionType = request.QuestionType;
        question.Points = request.Points;

        var requestOptionIds = request.Options
            .Where(o => o.Id.HasValue)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        var optionsToRemove = question.AnswerOptions
            .Where(a => !requestOptionIds.Contains(a.Id))
            .ToList();

        _context.AnswerOptions.RemoveRange(optionsToRemove);

        var order = 1;
        foreach (var optionRequest in request.Options)
        {
            if (optionRequest.Id.HasValue)
            {
                var existing = question.AnswerOptions
                    .FirstOrDefault(a => a.Id == optionRequest.Id.Value);

                if (existing is not null)
                {
                    existing.Text = _sanitizer.Sanitize(optionRequest.Text);
                    existing.IsCorrect = optionRequest.IsCorrect;
                    existing.Order = order;
                }
            }
            else
            {
                question.AnswerOptions.Add(new AnswerOption
                {
                    Text = _sanitizer.Sanitize(optionRequest.Text),
                    IsCorrect = optionRequest.IsCorrect,
                    Order = order
                });
            }

            order++;
        }
    }

    // ══════════════════════════════════════════
    //  PRIVATE — AUTH
    // ══════════════════════════════════════════
    private async Task<bool> HasInstructorPermissionAsync(int courseId, string userId)
    {
        var courseUser = await _context.CourseUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId);

        if (courseUser is null)
            return false;

        return (courseUser.PermissionMask & CoursePermissionMasks.CoInstructor) == CoursePermissionMasks.CoInstructor;
    }
}
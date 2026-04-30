using Ganss.Xss;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Exam;
using Neura.Core.Contracts.Question;
using Neura.Core.Enums;

namespace Neura.Services.Services;

public class ExamService : IExamService
{
    private readonly ApplicationDbContext _context;
    private readonly HtmlSanitizer _sanitizer;

    public ExamService(ApplicationDbContext context, HtmlSanitizer sanitizer)
    {
        _context = context;
        _sanitizer = sanitizer;
    }

    // ══════════════════════════════════════════
    //  CREATE
    // ══════════════════════════════════════════
    public async Task<Result<ExamResponse>> CreateAsync(CreateExamRequest request, string userId)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId);

        if (lesson is null)
            return Result.Failure<ExamResponse>(ExamErrors.LessonNotFound);

        var courseUser = _context.CourseUsers.Where(cu => cu.UserId == userId && cu.CourseId == lesson.Section.CourseId).FirstOrDefault();

        if (courseUser is null || !CoursePermissionMasks.HasPermission(courseUser.PermissionMask, CoursePermission.EditContent))
            return Result.Failure<ExamResponse>(ExamErrors.Forbidden);

        if (lesson.Type != LessonType.Quiz)
            return Result.Failure<ExamResponse>(ExamErrors.LessonNotQuizType);

        var courseId = lesson.Section.CourseId;
        //if (!await HasInstructorPermissionAsync(courseId, userId))
        //return Result.Failure<ExamResponse>(ExamErrors.Forbidden);

        var examExists = await _context.Exams
            .AnyAsync(e => e.LessonId == request.LessonId);

        if (examExists)
            return Result.Failure<ExamResponse>(ExamErrors.ExamAlreadyExists);

        var exam = request.Adapt<Exam>();
        exam.Title = _sanitizer.Sanitize(request.Title);
        exam.Description = request.Description is not null
            ? _sanitizer.Sanitize(request.Description)
            : null;
        exam.ShowCorrectAnswersAfterSubmit = true;
        exam.IsPublished = false;
        exam.CreatedById = userId;
        exam.CreatedOn = DateTime.UtcNow;

        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();

        var response = exam.Adapt<ExamResponse>();
        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  GET BY ID (Instructor — full detail)
    // ══════════════════════════════════════════
    public async Task<Result<ExamDetailResponse>> GetByIdAsync(int lessonId, string userId)
    {
        var exam = await _context.Exams
            .AsNoTracking()
            .Include(e => e.Questions.OrderBy(q => q.Order))
                .ThenInclude(q => q.AnswerOptions.OrderBy(a => a.Order))
            .Include(e => e.Attempts)
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId);

        if (exam is null)
            return Result.Failure<ExamDetailResponse>(ExamErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        //if (!await HasInstructorPermissionAsync(courseId, userId))
        //    return Result.Failure<ExamDetailResponse>(ExamErrors.Forbidden);

        var response = await BuildExamDetailResponseAsync(exam);
        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  GET BY LESSON ID (Instructor)
    // ══════════════════════════════════════════
    public async Task<Result<ExamDetailResponse>> GetByLessonIdAsync(int lessonId, string userId)
    {
        var exam = await _context.Exams
            .AsNoTracking()
            .Include(e => e.Questions.OrderBy(q => q.Order))
                .ThenInclude(q => q.AnswerOptions.OrderBy(a => a.Order))
            .Include(e => e.Attempts)
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId);

        if (exam is null)
            return Result.Failure<ExamDetailResponse>(ExamErrors.NoExamForLesson);

        var courseId = exam.Lesson.Section.CourseId;
        //if (!await HasInstructorPermissionAsync(courseId, userId))
        //    return Result.Failure<ExamDetailResponse>(ExamErrors.Forbidden);

        var response = await BuildExamDetailResponseAsync(exam);
        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  UPDATE SETTINGS
    // ══════════════════════════════════════════
    public async Task<Result<ExamResponse>> UpdateSettingsAsync(
        int lessonId, UpdateExamSettingsRequest request, string userId)
    {
        var exam = await _context.Exams
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId);

        if (exam is null)
            return Result.Failure<ExamResponse>(ExamErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        //if (!await HasInstructorPermissionAsync(courseId, userId))
        //    return Result.Failure<ExamResponse>(ExamErrors.Forbidden);

        exam.Title = _sanitizer.Sanitize(request.Title);
        exam.Description = request.Description is not null
            ? _sanitizer.Sanitize(request.Description)
            : null;
        exam.DurationInMinutes = request.DurationInMinutes;
        exam.PassingScorePercentage = request.PassingScorePercentage;
        exam.MaxAttempts = request.MaxAttempts;
        exam.ShuffleQuestions = request.ShuffleQuestions;
        exam.ShuffleAnswers = request.ShuffleAnswers;
        exam.NumberOfQuestionsToServe = request.NumberOfQuestionsToServe;
        exam.EnableTabSwitchDetection = request.EnableTabSwitchDetection;
        exam.MaxViolationsBeforeAutoSubmit = request.MaxViolationsBeforeAutoSubmit;
        exam.UpdatedById = userId;
        exam.UpdatedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var response = exam.Adapt<ExamResponse>();

        response.TotalQuestions = await _context.Questions
            .AsNoTracking()
            .CountAsync(q => q.ExamId == exam.Id);

        response.TotalPoints = await _context.Questions
            .AsNoTracking()
            .Where(q => q.ExamId == exam.Id)
            .SumAsync(q => q.Points);

        response.TotalAttempts = await _context.ExamAttempts
            .AsNoTracking()
            .CountAsync(a => a.ExamId == exam.Id);

        return Result.Success(response);
    }

    // ══════════════════════════════════════════
    //  PUBLISH
    // ══════════════════════════════════════════
    public async Task<Result> PublishAsync(int lessonId, string userId)
    {
        var exam = await _context.Exams
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .Include(e => e.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId);

        if (exam is null)
            return Result.Failure(ExamErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        //if (!await HasInstructorPermissionAsync(courseId, userId))
        //    return Result.Failure(ExamErrors.Forbidden);

        if (exam.IsPublished)
            return Result.Failure(ExamErrors.AlreadyPublished);

        if (!exam.Questions.Any())
            return Result.Failure(ExamErrors.NoQuestions);

        var hasInvalidQuestions = exam.Questions
            .Any(q => !q.AnswerOptions.Any(a => a.IsCorrect));

        if (hasInvalidQuestions)
            return Result.Failure(ExamErrors.QuestionsWithoutCorrectAnswer);

        if (exam.NumberOfQuestionsToServe.HasValue
            && exam.NumberOfQuestionsToServe.Value > exam.Questions.Count)
            return Result.Failure(ExamErrors.PoolSizeExceedsTotalQuestions);

        exam.IsPublished = true;
        exam.UpdatedById = userId;
        exam.UpdatedOn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    // ══════════════════════════════════════════
    //  UNPUBLISH
    // ══════════════════════════════════════════
    public async Task<Result> UnpublishAsync(int lessonId, string userId)
    {
        var exam = await _context.Exams
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId);

        if (exam is null)
            return Result.Failure(ExamErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        //if (!await HasInstructorPermissionAsync(courseId, userId))
        //    return Result.Failure(ExamErrors.Forbidden);

        if (!exam.IsPublished)
            return Result.Failure(ExamErrors.AlreadyUnpublished);

        var hasAttempts = await _context.ExamAttempts
            .AnyAsync(a => a.ExamId == exam.Id);

        if (hasAttempts)
            return Result.Failure(ExamErrors.CannotUnpublishWithAttempts);

        exam.IsPublished = false;
        exam.UpdatedById = userId;
        exam.UpdatedOn = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    // ══════════════════════════════════════════
    //  DELETE
    // ══════════════════════════════════════════
    public async Task<Result> DeleteAsync(int lessonId, string userId)
    {
        var exam = await _context.Exams
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == lessonId);

        if (exam is null)
            return Result.Failure(ExamErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;
        //if (!await HasInstructorPermissionAsync(courseId, userId))
        //    return Result.Failure(ExamErrors.Forbidden);

        var hasAttempts = await _context.ExamAttempts
            .AnyAsync(a => a.ExamId == exam.Id);

        if (hasAttempts)
            return Result.Failure(ExamErrors.CannotDeleteWithAttempts);

        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    // ══════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════
    //private async Task<bool> HasInstructorPermissionAsync(int courseId, string userId)
    //{
    //    var courseUser = await _context.CourseUsers
    //        .AsNoTracking()
    //        .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId);

    //    if (courseUser is null)
    //        return false;

    //    return (courseUser.PermissionMask & CoursePermissionMasks.CoInstructor) == CoursePermissionMasks.CoInstructor;
    //}

    private async Task<ExamDetailResponse> BuildExamDetailResponseAsync(Exam exam)
    {
        var questionIds = exam.Questions.Select(q => q.Id).ToList();

        var questionsWithAttempts = await _context.AttemptAnswers
            .AsNoTracking()
            .Where(aa => questionIds.Contains(aa.QuestionId))
            .Select(aa => aa.QuestionId)
            .Distinct()
            .ToHashSetAsync();

        var response = exam.Adapt<ExamDetailResponse>();

        response.Questions = exam.Questions.Select(q =>
        {
            var questionResponse = q.Adapt<QuestionResponse>();
            questionResponse.HasAttempts = questionsWithAttempts.Contains(q.Id);
            return questionResponse;
        }).ToList();

        return response;
    }
}
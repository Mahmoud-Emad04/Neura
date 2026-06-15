using MediatR;
using Neura.Core.Contracts.Analytics;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using System.Text.Json;

namespace Neura.Api.Features.ExamAnalytics.GetStudentOwnAttempts;

internal sealed class GetStudentOwnAttemptsHandler(ApplicationDbContext context)
    : IRequestHandler<GetStudentOwnAttemptsQuery, Result<ExamStudentAttemptsResponse>>
{
    public async Task<Result<ExamStudentAttemptsResponse>> Handle(
        GetStudentOwnAttemptsQuery query, CancellationToken ct)
    {
        var exam = await context.Exams
            .AsNoTracking()
            .Include(e => e.Lesson)
                .ThenInclude(l => l.Section)
            .FirstOrDefaultAsync(e => e.LessonId == query.ExamId, ct);

        if (exam is null)
            return Result.Failure<ExamStudentAttemptsResponse>(AnalyticsErrors.ExamNotFound);

        var courseId = exam.Lesson.Section.CourseId;

        var isEnrolled = await context.CourseUsers
            .AnyAsync(cu => cu.CourseId == courseId && cu.UserId == query.UserId, ct);

        if (!isEnrolled)
            return Result.Failure<ExamStudentAttemptsResponse>(AnalyticsErrors.Forbidden);

        var baseQuery = context.ExamAttempts
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Violations)
            .Where(a => a.ExamId == exam.Id && a.UserId == query.UserId && a.Status != AttemptStatus.InProgress);

        baseQuery = query.SortBy?.ToLower() switch
        {
            "score" => query.Descending
                ? baseQuery.OrderByDescending(a => a.ScorePercentage)
                : baseQuery.OrderBy(a => a.ScorePercentage),
            "duration" => query.Descending
                ? baseQuery.OrderByDescending(a =>
                    a.SubmittedAt.HasValue ? EF.Functions.DateDiffSecond(a.StartedAt, a.SubmittedAt.Value) : 0)
                : baseQuery.OrderBy(a =>
                    a.SubmittedAt.HasValue ? EF.Functions.DateDiffSecond(a.StartedAt, a.SubmittedAt.Value) : 0),
            "violations" => query.Descending
                ? baseQuery.OrderByDescending(a => a.Violations.Count)
                : baseQuery.OrderBy(a => a.Violations.Count),
            _ => query.Descending
                ? baseQuery.OrderByDescending(a => a.SubmittedAt)
                : baseQuery.OrderBy(a => a.SubmittedAt)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var totalCount = await baseQuery.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var attempts = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var attemptTotalPoints = new Dictionary<int, decimal>();
        foreach (var attempt in attempts)
        {
            var questionOrder = JsonSerializer.Deserialize<List<int>>(attempt.QuestionOrder) ?? new();
            var points = await context.Questions
                .AsNoTracking()
                .Where(q => questionOrder.Contains(q.Id))
                .SumAsync(q => q.Points, ct);
            attemptTotalPoints[attempt.Id] = points;
        }

        var attemptResponses = attempts.Select(a =>
        {
            // ── Security: Hide grades from students ──
            // Resolved attempts always show the overridden score.
            var shouldHideGrades = a.Status != AttemptStatus.Resolved
                && (!exam.AreGradesPublished
                    || a.Status == AttemptStatus.ViolationFlagged);

            return new StudentAttemptSummaryResponse
            {
                AttemptId = a.Id,
                UserId = a.UserId,
                StudentName = $"{a.User.FirstName} {a.User.LastName}".Trim(),
                StudentEmail = a.User.Email,
                Score = shouldHideGrades ? 0 : (a.Score ?? 0),
                ScorePercentage = shouldHideGrades ? 0 : (a.ScorePercentage ?? 0),
                TotalPoints = attemptTotalPoints.GetValueOrDefault(a.Id),
                Passed = shouldHideGrades ? false : (a.Passed ?? false),
                Status = a.Status.ToString(),
                StartedAt = a.StartedAt,
                SubmittedAt = a.SubmittedAt,
                DurationInSeconds = a.SubmittedAt.HasValue
                    ? (int)(a.SubmittedAt.Value - a.StartedAt).TotalSeconds
                    : null,
                ViolationCount = a.Violations.Count
            };
        }).ToList();

        return Result.Success(new ExamStudentAttemptsResponse
        {
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Attempts = attemptResponses
        });
    }
}

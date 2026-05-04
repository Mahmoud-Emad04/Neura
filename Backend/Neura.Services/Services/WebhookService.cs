using Neura.Core.Contracts.Webhook;
using Neura.Core.Enums;

namespace Neura.Services.Services;

public class WebhookService : IWebhookService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        ApplicationDbContext context,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> HandleCheatingAlertAsync(
        CheatingAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Parse exam ID
        if (!int.TryParse(request.ExamId, out var examId))
        {
            _logger.LogWarning("Invalid ExamId format: {ExamId}", request.ExamId);
            return Result.Failure(ExamErrors.InvalidExamId);
        }

        // 2. Verify exam exists
        var exam = await _context.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.LessonId == examId, cancellationToken);

        if (exam is null)
        {
            _logger.LogWarning("Cheating alert for non-existent exam {ExamId}", examId);
            return Result.Failure(ExamErrors.ExamNotFound);
        }

        // 3. Verify student exists
        var studentExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.StudentId, cancellationToken);

        if (!studentExists)
        {
            _logger.LogWarning("Cheating alert for non-existent student {StudentId}", request.StudentId);
            return Result.Failure(UserErrors.UserNotFound);
        }

        // 4. Find active attempt (optional - may not have one)
        var activeAttempt = await _context.ExamAttempts
            .FirstOrDefaultAsync(a => a.ExamId == exam.Id &&
                                     a.UserId == request.StudentId &&
                                     a.Status != AttemptStatus.Submitted,
                              cancellationToken);

        // 5. Convert Unix timestamp to UTC DateTime
        var detectedAt = DateTimeOffset
            .FromUnixTimeSeconds((long)request.Timestamp)
            .UtcDateTime;

        // 6. Create violation record
        var violation = new ExamViolation
        {
            ExamId = examId,
            StudentId = request.StudentId,
            ExamAttemptId = activeAttempt?.Id,
            Severity = request.Severity,
            Reason = request.Reason,
            DetectedAt = detectedAt
        };

        _context.ExamViolations.Add(violation);

        // 7. Auto-submit logic (if active attempt exists)
        if (activeAttempt is not null && ShouldAutoSubmit(exam, activeAttempt, request.Severity))
        {
            activeAttempt.Status = AttemptStatus.Submitted;
            activeAttempt.SubmittedAt = DateTime.UtcNow;
            violation.CausedAutoSubmit = true;

            _logger.LogWarning(
                "Auto-submitted attempt {AttemptId} for student {StudentId} due to {Severity} violation",
                activeAttempt.Id, request.StudentId, request.Severity);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "⚠️ Cheating alert recorded: Exam={ExamId}, Student={StudentId}, Severity={Severity}, Reason={Reason}",
            examId, request.StudentId, request.Severity, request.Reason);

        return Result.Success();
    }

    private async Task<bool> ShouldAutoSubmitAsync(
        Exam exam,
        ExamAttempt attempt,
        string severity,
        CancellationToken cancellationToken)
    {
        // Critical severity → auto-submit immediately
        if (severity.Equals("Critical", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check max violations threshold
        if (!exam.EnableTabSwitchDetection || !exam.MaxViolationsBeforeAutoSubmit.HasValue)
            return false;

        var violationsCount = await _context.ExamViolations
            .CountAsync(v => v.ExamAttemptId == attempt.Id, cancellationToken);

        return violationsCount + 1 >= exam.MaxViolationsBeforeAutoSubmit.Value;
    }

    private bool ShouldAutoSubmit(Exam exam, ExamAttempt attempt, string severity)
    {
        if (severity.Equals("Critical", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!exam.EnableTabSwitchDetection || !exam.MaxViolationsBeforeAutoSubmit.HasValue)
            return false;

        var violationsCount = _context.ExamViolations
            .Count(v => v.ExamAttemptId == attempt.Id);

        return violationsCount + 1 >= exam.MaxViolationsBeforeAutoSubmit.Value;
    }
}
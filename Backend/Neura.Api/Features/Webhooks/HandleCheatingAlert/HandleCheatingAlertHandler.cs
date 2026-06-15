using MediatR;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Webhooks.HandleCheatingAlert;

internal sealed class HandleCheatingAlertHandler(
    ApplicationDbContext context,
    IWebHostEnvironment environment,
    ILogger<HandleCheatingAlertHandler> logger)
    : IRequestHandler<HandleCheatingAlertCommand, Result>
{
    private const string ViolationFramesFolder = "Images/Violations";

    public async Task<Result> Handle(HandleCheatingAlertCommand command, CancellationToken ct)
    {
        var request = command.Request;

        // 1. Parse exam ID
        if (!int.TryParse(request.ExamId, out var examId))
        {
            logger.LogWarning("Invalid ExamId format: {ExamId}", request.ExamId);
            return Result.Failure(ExamErrors.InvalidExamId);
        }

        // 2. Verify exam exists
        var exam = await context.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.LessonId == examId, ct);

        if (exam is null)
        {
            logger.LogWarning("Cheating alert for non-existent exam {ExamId}", examId);
            return Result.Failure(ExamErrors.ExamNotFound);
        }

        // 3. Verify student exists
        var studentExists = await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.StudentId, ct);

        if (!studentExists)
        {
            logger.LogWarning("Cheating alert for non-existent student {StudentId}", request.StudentId);
            return Result.Failure(UserErrors.UserNotFound);
        }

        // 4. Find active attempt (optional — may not have one)
        var activeAttempt = await context.ExamAttempts
            .FirstOrDefaultAsync(a => a.ExamId == exam.Id &&
                                      a.UserId == request.StudentId &&
                                      a.Status != AttemptStatus.Submitted, ct);

        // 5. Convert Unix timestamp to UTC DateTime
        var detectedAt = DateTimeOffset
            .FromUnixTimeSeconds((long)request.Timestamp)
            .UtcDateTime;

        // 6. Decode and save the suspicious frame (if provided)
        string? frameImagePath = null;

        if (!string.IsNullOrEmpty(request.FrameData))
        {
            try
            {
                var imageBytes = Convert.FromBase64String(request.FrameData);
                var fileName = $"{examId}_{request.StudentId}_{detectedAt:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.jpg";
                var folderPath = Path.Combine(environment.WebRootPath, ViolationFramesFolder);

                Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                await File.WriteAllBytesAsync(filePath, imageBytes, ct);

                frameImagePath = $"{ViolationFramesFolder}/{fileName}";

                logger.LogInformation(
                    "Saved suspicious frame for Exam={ExamId}, Student={StudentId} at {Path}",
                    examId, request.StudentId, frameImagePath);
            }
            catch (FormatException ex)
            {
                logger.LogWarning(ex, "Invalid Base64 frameData for Exam={ExamId}, Student={StudentId}",
                    examId, request.StudentId);
                // Continue processing — frame storage failure should not reject the alert
            }
        }

        // 7. Create violation record
        var violation = new ExamViolation
        {
            ExamId = exam.Id,
            StudentId = request.StudentId,
            ExamAttemptId = activeAttempt?.Id,
            Severity = request.Severity,
            Reason = request.Reason,
            DetectedAt = detectedAt,
            FrameImagePath = frameImagePath,
            CreatedById = request.StudentId
        };

        context.ExamViolations.Add(violation);

        // 8. Auto-submit logic
        //if (activeAttempt is not null && ShouldAutoSubmit(exam, activeAttempt, request.Severity))
        //{
        //    activeAttempt.Status = AttemptStatus.Submitted;
        //    activeAttempt.SubmittedAt = DateTime.UtcNow;
        //    violation.CausedAutoSubmit = true;

        //    logger.LogWarning(
        //        "Auto-submitted attempt {AttemptId} for student {StudentId} due to {Severity} violation",
        //        activeAttempt.Id, request.StudentId, request.Severity);
        //}

        await context.SaveChangesAsync(ct);

        logger.LogWarning(
            "⚠️ Cheating alert recorded: Exam={ExamId}, Student={StudentId}, Severity={Severity}, Reason={Reason}",
            examId, request.StudentId, request.Severity, request.Reason);

        return Result.Success();
    }

    private static bool ShouldAutoSubmit(Exam exam, ExamAttempt attempt, string severity)
    {
        if (severity.Equals("Critical", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!exam.EnableTabSwitchDetection || !exam.MaxViolationsBeforeAutoSubmit.HasValue)
            return false;

        var violationsCount = attempt.Violations?.Count ?? 0;
        return violationsCount + 1 >= exam.MaxViolationsBeforeAutoSubmit.Value;
    }
}

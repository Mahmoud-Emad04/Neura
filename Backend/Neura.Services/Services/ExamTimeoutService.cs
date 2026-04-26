using Neura.Core.Enums;

namespace Neura.Services.Services;

public class ExamTimeoutService : IExamTimeoutService
{
    private readonly ApplicationDbContext _context;
    private readonly IGradingService _gradingService;
    private readonly ILogger<ExamTimeoutService> _logger;

    private const int BatchSize = 50;

    public ExamTimeoutService(
        ApplicationDbContext context,
        IGradingService gradingService,
        ILogger<ExamTimeoutService> logger)
    {
        _context = context;
        _gradingService = gradingService;
        _logger = logger;
    }

    public async Task ProcessTimedOutAttemptsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting timed-out attempt processing at {Time}", DateTime.UtcNow);

        var totalProcessed = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            // Fetch InProgress attempts whose time has expired
            var expiredAttempts = await _context.ExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.AttemptAnswers)
                    .ThenInclude(aa => aa.SelectedOptions)
                .Where(a => a.Status == AttemptStatus.InProgress
                         && a.Exam.DurationInMinutes != null
                         && now > a.StartedAt.AddMinutes(a.Exam.DurationInMinutes!.Value))
                .OrderBy(a => a.StartedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (!expiredAttempts.Any())
                break;

            foreach (var attempt in expiredAttempts)
            {
                try
                {
                    await _gradingService.GradeAttemptAsync(attempt, AttemptStatus.TimedOut);
                    totalProcessed++;

                    _logger.LogInformation(
                        "Auto-closed timed-out attempt {AttemptId} for user {UserId} on exam {ExamId}. " +
                        "Score: {Score} ({Percentage}%)",
                        attempt.Id,
                        attempt.UserId,
                        attempt.ExamId,
                        attempt.Score,
                        attempt.ScorePercentage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to auto-close attempt {AttemptId} for user {UserId} on exam {ExamId}",
                        attempt.Id,
                        attempt.UserId,
                        attempt.ExamId);
                }
            }
        }

        _logger.LogInformation(
            "Finished timed-out attempt processing. Total processed: {Count}",
            totalProcessed);
    }
}
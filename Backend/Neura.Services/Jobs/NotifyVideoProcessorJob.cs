using Microsoft.Extensions.Logging;
using Neura.Core.Services;
using Neura.Repository.Persistence;

namespace Neura.Services.Jobs;

public class NotifyVideoProcessorJob
{
    private readonly IExternalVideoProcessor _processor;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotifyVideoProcessorJob> _logger;

    public NotifyVideoProcessorJob(
        IExternalVideoProcessor processor,
        ApplicationDbContext context,
        ILogger<NotifyVideoProcessorJob> logger)
    {
        _processor = processor;
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(int lessonId, string downloadUrl)
    {
        try
        {
            await _processor.ProcessVideoAsync(lessonId, downloadUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify external video processor for LessonId={LessonId}", lessonId);

            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson != null)
            {
                lesson.MarkVideoProcessingFailed();
                await _context.SaveChangesAsync();
            }

            throw; // Re-throw so Hangfire knows the job failed and can retry
        }
    }
}

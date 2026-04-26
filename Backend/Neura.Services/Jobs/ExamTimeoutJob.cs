namespace Neura.Services.Jobs;

public class ExamTimeoutJob
{
    private readonly IExamTimeoutService _timeoutService;

    public ExamTimeoutJob(IExamTimeoutService timeoutService)
    {
        _timeoutService = timeoutService;
    }

    public async Task ExecuteAsync()
    {
        await _timeoutService.ProcessTimedOutAttemptsAsync();
    }
}
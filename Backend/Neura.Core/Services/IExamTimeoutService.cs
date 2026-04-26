namespace Neura.Core.Services;

public interface IExamTimeoutService
{
    Task ProcessTimedOutAttemptsAsync(CancellationToken cancellationToken = default);
}
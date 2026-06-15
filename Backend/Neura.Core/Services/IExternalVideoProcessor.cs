namespace Neura.Core.Services;

public interface IExternalVideoProcessor
{
    Task ProcessVideoAsync(int lessonId, string downloadUrl, CancellationToken ct = default);
}

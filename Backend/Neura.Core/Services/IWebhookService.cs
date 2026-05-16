using Neura.Core.Abstractions;
using Neura.Core.Contracts.Webhook;

namespace Neura.Core.Services;

public interface IWebhookService
{
    Task<Result> HandleCheatingAlertAsync(
      CheatingAlertRequest request,
      CancellationToken cancellationToken = default);
}

using Neura.Api.Filters;
using Neura.Core.Contracts.Webhook;

namespace Neura.Api.Controllers;

[Route("api/webhooks")]
[ApiController]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;

    public WebhooksController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost("cheating_alert")]
    [ValidateWebhookSecret]
    public async Task<IActionResult> CheatingAlert(
        [FromBody] CheatingAlertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _webhookService.HandleCheatingAlertAsync(request, cancellationToken);

        return result.IsSuccess
            ? Ok(new { status = "success" })
            : result.ToProblem();
    }
}
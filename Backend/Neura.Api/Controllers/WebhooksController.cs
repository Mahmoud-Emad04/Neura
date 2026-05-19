using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Neura.Api.Filters;
using Neura.Core.Contracts.Webhook;
using Neura.Api.Extensions;
using Neura.Api.Features.Webhooks.HandleCheatingAlert;

namespace Neura.Api.Controllers;

[Route("api/webhooks")]
[ApiController]
[AllowAnonymous]
public class WebhooksController(ISender sender) : ControllerBase
{
    [HttpPost("cheating_alert")]
    [ValidateWebhookSecret]
    public async Task<IActionResult> CheatingAlert(
        [FromBody] CheatingAlertRequest request,
        CancellationToken ct)
    {
        var command = new HandleCheatingAlertCommand(request);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(new { status = "success" })
            : result.ToProblem();
    }
}
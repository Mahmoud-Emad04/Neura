using MediatR;
using Neura.Api.Features.Webhooks.HandleCheatingAlert;
using Neura.Api.Features.Webhooks.HandleVideoTranscription;
using Neura.Api.Filters;
using Neura.Core.Contracts.Webhook;

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

    [HttpPost("video_transcription")]
    //[ValidateWebhookSecret]
    public async Task<IActionResult> VideoTranscription(
        [FromBody] VideoTranscriptionRequest request,
        CancellationToken ct)
    {
        var command = new HandleVideoTranscriptionCommand(request);
        var result = await sender.Send(command, ct);

        return result.IsSuccess
            ? Ok(new { status = "success" })
            : result.ToProblem();
    }
}
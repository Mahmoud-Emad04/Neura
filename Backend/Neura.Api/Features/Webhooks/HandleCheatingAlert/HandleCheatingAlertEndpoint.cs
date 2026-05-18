using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Neura.Api.Infrastructure;
using Neura.Core.Contracts.Webhook;

namespace Neura.Api.Features.Webhooks.HandleCheatingAlert;

public sealed class HandleCheatingAlertEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/webhooks/cheating_alert", async (
            CheatingAlertRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new HandleCheatingAlertCommand(request);
            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(new { status = "success" })
                : result.ToProblemMinimal();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidateWebhookSecretFilter>()
        .WithTags("Webhooks")
        .WithName("HandleCheatingAlert");
    }
}

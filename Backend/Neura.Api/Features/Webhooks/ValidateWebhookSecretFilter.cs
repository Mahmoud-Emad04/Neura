using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Neura.Api.Features.Webhooks;

/// <summary>
/// Minimal API endpoint filter that validates the X-Webhook-Secret header.
/// </summary>
public sealed class ValidateWebhookSecretFilter(IConfiguration configuration) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expectedSecret = configuration["Webhooks:CheatingAlertSecret"];

        if (string.IsNullOrEmpty(expectedSecret))
            return Results.StatusCode(500);

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Webhook-Secret", out var providedSecret)
            || providedSecret != expectedSecret)
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}

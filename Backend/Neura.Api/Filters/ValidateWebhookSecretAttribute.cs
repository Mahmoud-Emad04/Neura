using Microsoft.AspNetCore.Mvc.Filters;

namespace Neura.Api.Filters;

public class ValidateWebhookSecretAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>();

        var expectedSecret = configuration["Webhooks:CheatingAlertSecret"];

        if (string.IsNullOrEmpty(expectedSecret))
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Webhook-Secret", out var providedSecret) ||
            providedSecret != expectedSecret)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid webhook secret" });
            return;
        }

        await next();
    }
}
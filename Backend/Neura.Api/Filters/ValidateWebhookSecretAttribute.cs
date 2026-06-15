using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace Neura.Api.Filters;

public class ValidateWebhookSecretAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>();

        var hmacSecret = configuration["Webhooks:HmacSecret"];

        if (string.IsNullOrEmpty(hmacSecret))
        {
            context.Result = new StatusCodeResult(statusCode: 500);
            return;
        }

        // 1. Read the X-Signature-SHA256 header
        var signatureHeader = context.HttpContext.Request.Headers["X-Signature-SHA256"].ToString();

        if (string.IsNullOrEmpty(signatureHeader) || !signatureHeader.StartsWith("sha256="))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Missing or invalid signature header." });
            return;
        }

        var providedSignature = signatureHeader.Substring(7); // Remove "sha256=" prefix

        // 2. Read raw body bytes (buffering must be enabled in middleware/controller)
        context.HttpContext.Request.Body.Position = 0;
        using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        context.HttpContext.Request.Body.Position = 0; // Reset for downstream model binding

        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

        // 3. Compute HMAC-SHA256
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hmacSecret));
        var computedHash = hmac.ComputeHash(bodyBytes);
        var computedSignature = BitConverter.ToString(computedHash).Replace("-", "").ToLower();

        // 4. Compare signatures
        if (!string.Equals(providedSignature, computedSignature, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Signature mismatch." });
            return;
        }

        await next();
    }
}
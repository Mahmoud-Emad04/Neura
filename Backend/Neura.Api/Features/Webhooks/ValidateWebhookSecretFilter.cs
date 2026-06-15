using System.Security.Cryptography;
using System.Text;

namespace Neura.Api.Features.Webhooks;

/// <summary>
/// Minimal API endpoint filter that validates the HMAC-SHA256 signature
/// from the X-Signature-SHA256 header.
/// </summary>
public sealed class ValidateWebhookSecretFilter(IConfiguration configuration) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var hmacSecret = configuration["Webhooks:HmacSecret"];

        if (string.IsNullOrEmpty(hmacSecret))
            return Results.StatusCode(500);

        // 1. Read the X-Signature-SHA256 header
        var signatureHeader = context.HttpContext.Request.Headers["X-Signature-SHA256"].ToString();

        if (string.IsNullOrEmpty(signatureHeader) || !signatureHeader.StartsWith("sha256="))
            return Results.Unauthorized();

        var providedSignature = signatureHeader.Substring(7); // Remove "sha256=" prefix

        // 2. Read raw body bytes
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
            return Results.Unauthorized();

        return await next(context);
    }
}

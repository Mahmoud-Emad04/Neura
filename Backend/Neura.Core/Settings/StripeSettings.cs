namespace Neura.Core.Settings;

/// <summary>
/// Configuration settings for Stripe payment integration.
/// </summary>
public class StripeSettings
{
	public const string SectionName = "Stripe";

	/// <summary>
	/// Stripe Secret Key (server-side, keep secure).
	/// </summary>
	public string SecretKey { get; set; } = string.Empty;

	/// <summary>
	/// Stripe Publishable Key (safe for client-side).
	/// </summary>
	public string PublishableKey { get; set; } = string.Empty;

	/// <summary>
	/// Stripe Webhook signing secret for verifying webhook events.
	/// </summary>
	public string WebhookSecret { get; set; } = string.Empty;

	/// <summary>
	/// Default currency for payments (e.g., "usd").
	/// </summary>
	public string Currency { get; set; } = "usd";

	/// <summary>
	/// Frontend URL to redirect after successful payment.
	/// </summary>
	public string SuccessUrl { get; set; } = string.Empty;

	/// <summary>
	/// Frontend URL to redirect after cancelled payment.
	/// </summary>
	public string CancelUrl { get; set; } = string.Empty;

	/// <summary>
	/// Validate settings have required values.
	/// </summary>
	public bool IsValid() =>
		!string.IsNullOrWhiteSpace(SecretKey) &&
		!string.IsNullOrWhiteSpace(WebhookSecret);
}

namespace Neura.Core.Contracts.Payment;

public class CreateCheckoutSessionResponse
{
    /// <summary>
    ///     The Stripe Checkout Session ID (used by Stripe.js to redirect).
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    ///     The Stripe Checkout Session URL (direct redirect).
    /// </summary>
    public string SessionUrl { get; set; } = string.Empty;

    /// <summary>
    ///     The Stripe publishable key for client-side initialization.
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;
}

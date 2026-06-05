using Neura.Core.Abstractions;
using Neura.Core.Contracts.Payment;

namespace Neura.Core.Services;

/// <summary>
///     Abstraction for Stripe payment operations.
/// </summary>
public interface IStripeService
{
    /// <summary>
    ///     Creates a Stripe Checkout Session for a course purchase.
    /// </summary>
    Task<Result<CreateCheckoutSessionResponse>> CreateCheckoutSessionAsync(
        string userId,
        string userEmail,
        int courseId,
        string courseTitle,
        int priceInCents,
        CancellationToken ct = default);

    /// <summary>
    ///     Processes a Stripe webhook event (checkout.session.completed, etc.).
    ///     Returns the session ID if successful.
    /// </summary>
    Task<Result<string>> HandleWebhookAsync(string json, string stripeSignature, CancellationToken ct = default);
}

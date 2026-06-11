namespace Neura.Core.Contracts.Payment;

/// <summary>
///     Request body for the internal webhook-confirm endpoint.
///     Sent by the Stripe webhook handler after successful payment
///     to double-confirm enrollment on the production server.
/// </summary>
public sealed record WebhookConfirmRequest(string CourseId, string UserId);

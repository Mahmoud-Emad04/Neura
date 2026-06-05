namespace Neura.Core.Entities;

/// <summary>
///     Tracks payment transactions for course enrollments via Stripe.
/// </summary>
public class Payment
{
    public int Id { get; set; }

    /// <summary>
    ///     The Stripe Checkout Session ID.
    /// </summary>
    public string StripeSessionId { get; set; } = string.Empty;

    /// <summary>
    ///     The Stripe Payment Intent ID (set after payment succeeds).
    /// </summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    ///     Amount paid in the smallest currency unit (e.g., cents for USD).
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    ///     Currency code (e.g., "usd").
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    ///     Payment status: Pending, Completed, Failed, Refunded.
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    ///     The user who made the payment.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;

    /// <summary>
    ///     The course being purchased.
    /// </summary>
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedOn { get; set; }
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}

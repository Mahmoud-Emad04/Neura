using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Payment;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Services;
using Neura.Core.Settings;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;
using Stripe;
using Stripe.Checkout;
using System.Net.Http.Json;

namespace Neura.Services.Services;

public class StripeService : IStripeService
{
    private readonly ApplicationDbContext _context;
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceHelpers _helpers;

    public StripeService(
        ApplicationDbContext context,
        IOptions<StripeSettings> settings,
        ILogger<StripeService> logger,
        IHttpClientFactory httpClientFactory,
        IServiceHelpers helpers)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _helpers = helpers;

        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<Result<CreateCheckoutSessionResponse>> CreateCheckoutSessionAsync(
        string userId,
        string userEmail,
        int courseId,
        string courseTitle,
        int priceInCents,
        CancellationToken ct = default)
    {
        try
        {
            var sessionService = new SessionService();

            // Check if there is an existing pending payment
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.CourseId == courseId
                                          && p.UserId == userId
                                          && p.Status == PaymentStatus.Pending, ct);

            if (existingPayment != null)
            {
                var existingSession = await sessionService.GetAsync(existingPayment.StripeSessionId, cancellationToken: ct);
                
                if (existingSession.Status == "open")
                {
                    _logger.LogInformation("Reusing existing open Stripe session {SessionId} for user {UserId}, course {CourseId}",
                        existingSession.Id, userId, courseId);
                    
                    return Result.Success(new CreateCheckoutSessionResponse
                    {
                        SessionId = existingSession.Id,
                        SessionUrl = existingSession.Url,
                        PublishableKey = _settings.PublishableKey
                    });
                }
                else
                {
                    // Update status if it's expired or completed so we don't pick it up again
                    existingPayment.Status = existingSession.Status == "complete" ? PaymentStatus.Completed : PaymentStatus.Failed;
                    await _context.SaveChangesAsync(ct);
                }
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = ["card"],
                Mode = "payment",
                CustomerEmail = userEmail,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _settings.Currency,
                            UnitAmount = priceInCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = courseTitle,
                                Description = $"Enrollment in: {courseTitle}"
                            }
                        },
                        Quantity = 1
                    }
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "courseId", courseId.ToString() }
                },
                SuccessUrl = $"{_settings.SuccessUrl}?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = _settings.CancelUrl
            };

            var session = await sessionService.CreateAsync(options, cancellationToken: ct);

            // Create a pending payment record
            var payment = new Payment
            {
                StripeSessionId = session.Id,
                Amount = priceInCents,
                Currency = _settings.Currency,
                Status = PaymentStatus.Pending,
                UserId = userId,
                CourseId = courseId,
                CreatedOn = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Created Stripe checkout session {SessionId} for user {UserId}, course {CourseId}",
                session.Id, userId, courseId);

            return Result.Success(new CreateCheckoutSessionResponse
            {
                SessionId = session.Id,
                SessionUrl = session.Url,
                PublishableKey = _settings.PublishableKey
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session for user {UserId}, course {CourseId}",
                userId, courseId);
            return Result.Failure<CreateCheckoutSessionResponse>(
                new Error("Payment.StripeError", $"Payment service error: {ex.Message}", 502));
        }
    }

    public async Task<Result<string>> HandleWebhookAsync(
        string json,
        string stripeSignature,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Webhook received. Signature present: {HasSig}, Body length: {Len}",
            !string.IsNullOrEmpty(stripeSignature), json?.Length ?? 0);

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _settings.WebhookSecret);

            _logger.LogInformation("Stripe event verified: Type={EventType}, Id={EventId}",
                stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    _logger.LogInformation("Processing checkout.session.completed");
                    await HandleCheckoutSessionCompleted(stripeEvent, ct);
                    break;

                case "checkout.session.expired":
                    _logger.LogInformation("Processing checkout.session.expired");
                    await HandleCheckoutSessionExpired(stripeEvent, ct);
                    break;

                default:
                    _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Result.Success(stripeEvent.Id);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed. WebhookSecret starts with: {SecretPrefix}",
                _settings.WebhookSecret?.Length > 10 ? _settings.WebhookSecret[..10] + "..." : "TOO_SHORT");
            return Result.Failure<string>(
                new Error("Payment.WebhookError", $"Webhook error: {ex.Message}", 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe webhook");
            return Result.Failure<string>(
                new Error("Payment.WebhookError", $"Internal error: {ex.Message}", 500));
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null)
        {
            _logger.LogWarning("Could not cast event data to Session. Data type: {DataType}",
                stripeEvent.Data.Object?.GetType().FullName ?? "null");
            return;
        }

        _logger.LogInformation("Session ID: {SessionId}, PaymentStatus: {PaymentStatus}, PaymentIntentId: {PaymentIntentId}",
            session.Id, session.PaymentStatus, session.PaymentIntentId);

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripeSessionId == session.Id, ct);

        if (payment is null)
        {
            _logger.LogWarning("Payment record not found for session {SessionId}", session.Id);
            return;
        }

        _logger.LogInformation("Found payment record: Id={PaymentId}, UserId={UserId}, CourseId={CourseId}, CurrentStatus={Status}",
            payment.Id, payment.UserId, payment.CourseId, payment.Status);

        payment.StripePaymentIntentId = session.PaymentIntentId;
        payment.Status = PaymentStatus.Completed;
        payment.CompletedOn = DateTime.UtcNow;

        // Auto-enroll the user in the course
        var existingEnrollment = await _context.CourseUsers
            .FirstOrDefaultAsync(cu => cu.CourseId == payment.CourseId && cu.UserId == payment.UserId, ct);

        if (existingEnrollment is not null)
        {
            if (existingEnrollment.IsDeleted)
            {
                existingEnrollment.IsDeleted = false;
                existingEnrollment.EnrolledOn = DateTime.UtcNow;
                existingEnrollment.LastAccessedOn = DateTime.UtcNow;
                _logger.LogInformation("Re-activated deleted enrollment for user {UserId}", payment.UserId);
            }
            else
            {
                _logger.LogInformation("User {UserId} already enrolled in course {CourseId}, skipping",
                    payment.UserId, payment.CourseId);
            }
        }
        else
        {
            var studentRole = await _context.CourseRoles
                .FirstAsync(r => r.Level == (int)CourseRoleType.Student, ct);

            var courseUser = new CourseUser
            {
                CourseId = payment.CourseId,
                UserId = payment.UserId,
                CourseRoleId = studentRole.Id,
                PermissionMask = studentRole.PermissionMask,
                EnrolledOn = DateTime.UtcNow,
                LastAccessedOn = DateTime.UtcNow
            };

            _context.CourseUsers.Add(courseUser);
            _logger.LogInformation("Creating new enrollment for user {UserId} in course {CourseId}",
                payment.UserId, payment.CourseId);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "✅ Payment completed and user {UserId} enrolled in course {CourseId} via session {SessionId}",
            payment.UserId, payment.CourseId, session.Id);

        // Fire-and-forget: call production webhook-confirm endpoint to double-confirm enrollment
        await CallProductionWebhookConfirmAsync(payment.CourseId, payment.UserId, ct);
    }

    private async Task HandleCheckoutSessionExpired(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null) return;

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripeSessionId == session.Id, ct);

        if (payment is null) return;

        payment.Status = PaymentStatus.Failed;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Checkout session {SessionId} expired", session.Id);
    }

    /// <summary>
    ///     Calls the production server's webhook-confirm endpoint
    ///     to double-confirm enrollment after a successful payment.
    /// </summary>
    private async Task CallProductionWebhookConfirmAsync(int courseId, string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.ProductionBaseUrl))
        {
            _logger.LogWarning("ProductionBaseUrl is not configured — skipping webhook-confirm callback");
            return;
        }

        try
        {
            var courseKeyId = _helpers.Encode(courseId);
            var url = $"{_settings.ProductionBaseUrl.TrimEnd('/')}/api/payments/webhook-confirm";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Webhook-Secret", _settings.WebhookSecret);

            var body = new WebhookConfirmRequest(courseKeyId, userId);
            var response = await client.PostAsJsonAsync(url, body, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "✅ Production webhook-confirm succeeded for user {UserId}, course {CourseId}",
                    userId, courseId);
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "⚠️ Production webhook-confirm returned {StatusCode} for user {UserId}, course {CourseId}: {Body}",
                    response.StatusCode, userId, courseId, responseBody);
            }
        }
        catch (Exception ex)
        {
            // Don't let a callback failure break the webhook response to Stripe
            _logger.LogError(ex,
                "❌ Failed to call production webhook-confirm for user {UserId}, course {CourseId}",
                userId, courseId);
        }
    }
}

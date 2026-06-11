using MediatR;
using Neura.Api.Features.Payment.CreateCheckoutSession;
using Neura.Api.Features.Payment.HandleStripeWebhook;
using Neura.Api.Features.Payment.VerifyPayment;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Contracts.Payment;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Create a Stripe Checkout Session for a paid course.
    ///     Returns session URL for client-side redirect.
    /// </summary>
    [HttpPost("checkout/{courseId}")]
    [Authorize]
    [ProducesResponseType(typeof(CreateCheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCheckoutSession(string courseId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new CreateCheckoutSessionCommand(courseId, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Verify payment and enroll the user.
    ///     Polls Stripe directly to check if the pending session was paid.
    ///     Use this after returning from Stripe Checkout to confirm enrollment.
    /// </summary>
    [HttpPost("verify/{courseId}")]
    [Authorize]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyPayment(string courseId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new VerifyPaymentCommand(courseId, userId);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Stripe webhook endpoint.
    ///     Called by Stripe when payment events occur (e.g., checkout completed).
    ///     Must be publicly accessible (no [Authorize]).
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StripeWebhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrEmpty(stripeSignature))
            return BadRequest("Missing Stripe-Signature header");

        var command = new HandleStripeWebhookCommand(json, stripeSignature);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }
}


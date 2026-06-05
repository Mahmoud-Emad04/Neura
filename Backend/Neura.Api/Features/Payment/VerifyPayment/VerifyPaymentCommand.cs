using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Enrollment;

namespace Neura.Api.Features.Payment.VerifyPayment;

/// <summary>
///     Manually verify a Stripe session and complete enrollment if payment succeeded.
///     Useful when webhooks haven't fired yet (e.g., Stripe CLI not running).
/// </summary>
public sealed record VerifyPaymentCommand(string CourseId, string UserId)
    : IRequest<Result<EnrollmentResponse>>;

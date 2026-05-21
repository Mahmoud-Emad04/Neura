using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Payment;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.Services;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Payment.CreateCheckoutSession;

internal sealed class CreateCheckoutSessionHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IStripeService stripeService,
    IServiceHelpers helpers)
    : IRequestHandler<CreateCheckoutSessionCommand, Result<CreateCheckoutSessionResponse>>
{
    public async Task<Result<CreateCheckoutSessionResponse>> Handle(
        CreateCheckoutSessionCommand request, CancellationToken ct)
    {
        // Validate user
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return Result.Failure<CreateCheckoutSessionResponse>(PaymentErrors.UserNotFound);

        if (!user.EmailConfirmed)
            return Result.Failure<CreateCheckoutSessionResponse>(PaymentErrors.EmailNotVerified);

        // Decode course ID
        if (!TryDecodeCourseId(request.CourseId, out var courseId))
            return Result.Failure<CreateCheckoutSessionResponse>(PaymentErrors.CourseNotFound);

        // Validate course
        var course = await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted, ct);

        if (course is null)
            return Result.Failure<CreateCheckoutSessionResponse>(PaymentErrors.CourseNotFound);

        if (course.Status == CourseStatus.Pending)
            return Result.Failure<CreateCheckoutSessionResponse>(PaymentErrors.CourseNotActive);

        if (course.Price <= 0)
            return Result.Failure<CreateCheckoutSessionResponse>(PaymentErrors.CourseIsFree);

        // Check if already enrolled
        var existingEnrollment = await context.CourseUsers
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == request.UserId && !cu.IsDeleted, ct);

        if (existingEnrollment is not null)
            return Result.Failure<CreateCheckoutSessionResponse>(PaymentErrors.AlreadyEnrolled);

        // Check for existing pending payment
        var pendingPayment = await context.Payments
            .FirstOrDefaultAsync(p => p.CourseId == courseId
                                      && p.UserId == request.UserId
                                      && p.Status == PaymentStatus.Pending, ct);

        if (pendingPayment is not null)
            return Result.Failure<CreateCheckoutSessionResponse>(PaymentErrors.PaymentPending);

        // Price is stored as whole number (e.g., 50 = $50),
        // Stripe expects amount in cents (e.g., 5000 = $50.00)
        var priceInCents = course.Price * 100;

        return await stripeService.CreateCheckoutSessionAsync(
            request.UserId,
            user.Email!,
            courseId,
            course.Title,
            priceInCents,
            ct);
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }
        courseId = numbers[0];
        return true;
    }
}

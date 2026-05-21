using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;
using Stripe.Checkout;

namespace Neura.Api.Features.Payment.VerifyPayment;

internal sealed class VerifyPaymentHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers,
    ILogger<VerifyPaymentHandler> logger)
    : IRequestHandler<VerifyPaymentCommand, Result<EnrollmentResponse>>
{
    public async Task<Result<EnrollmentResponse>> Handle(
        VerifyPaymentCommand request, CancellationToken ct)
    {
        if (!TryDecodeCourseId(request.CourseId, out var courseId))
            return Result.Failure<EnrollmentResponse>(PaymentErrors.CourseNotFound);

        // Find the pending payment for this user + course
        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.CourseId == courseId
                                      && p.UserId == request.UserId
                                      && p.Status == PaymentStatus.Pending, ct);

        if (payment is null)
            return Result.Failure<EnrollmentResponse>(
                new Error("Payment.NoPendingPayment", "No pending payment found for this course", 404));

        // Check the session status directly with Stripe
        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(payment.StripeSessionId, cancellationToken: ct);

        if (session.PaymentStatus != "paid")
            return Result.Failure<EnrollmentResponse>(
                new Error("Payment.NotPaid", $"Payment not completed yet. Status: {session.PaymentStatus}", 402));

        // Payment is confirmed — update payment record
        payment.StripePaymentIntentId = session.PaymentIntentId;
        payment.Status = PaymentStatus.Completed;
        payment.CompletedOn = DateTime.UtcNow;

        // Enroll the user
        var existingEnrollment = await context.CourseUsers
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == request.UserId, ct);

        if (existingEnrollment is not null)
        {
            if (existingEnrollment.IsDeleted)
            {
                existingEnrollment.IsDeleted = false;
                existingEnrollment.EnrolledOn = DateTime.UtcNow;
                existingEnrollment.LastAccessedOn = DateTime.UtcNow;
            }
        }
        else
        {
            var studentRole = await context.CourseRoles
                .FirstAsync(r => r.Level == (int)CourseRoleType.Student, ct);

            var courseUser = new CourseUser
            {
                CourseId = courseId,
                UserId = request.UserId,
                CourseRoleId = studentRole.Id,
                PermissionMask = studentRole.PermissionMask,
                EnrolledOn = DateTime.UtcNow,
                LastAccessedOn = DateTime.UtcNow
            };

            context.CourseUsers.Add(courseUser);
        }

        await context.SaveChangesAsync(ct);

        var course = await context.Courses.FirstAsync(c => c.Id == courseId, ct);
        var user = await context.Users.FirstAsync(u => u.Id == request.UserId, ct);
        var role = await context.CourseRoles.FirstAsync(r => r.Level == (int)CourseRoleType.Student, ct);

        logger.LogInformation(
            "Payment verified and user {UserId} enrolled in course {CourseId} via session {SessionId}",
            request.UserId, courseId, payment.StripeSessionId);

        return Result.Success(new EnrollmentResponse
        {
            CourseId = course.Id,
            CourseName = course.Title,
            CourseThumbnail = course.ImageUrl,
            UserId = user.Id,
            UserName = $"{user.FirstName} {user.LastName}",
            Role = CourseRoleType.Student,
            RoleName = role.Name,
            EnrolledOn = DateTime.UtcNow,
            LastAccessedOn = DateTime.UtcNow,
            IsActive = true
        });
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

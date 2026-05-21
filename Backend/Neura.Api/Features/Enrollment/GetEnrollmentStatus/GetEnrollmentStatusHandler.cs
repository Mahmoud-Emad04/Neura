using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Errors;
using Neura.Core.Contracts.Enrollment;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Enrollment.GetEnrollmentStatus;

internal sealed class GetEnrollmentStatusHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IServiceHelpers helpers) 
    : IRequestHandler<GetEnrollmentStatusQuery, Result<EnrollmentStatusResponse>>
{
    public async Task<Result<EnrollmentStatusResponse>> Handle(
        GetEnrollmentStatusQuery request, CancellationToken ct)
    {
        if (!TryDecodeCourseId(request.CourseIdKey, out var courseId))
            return Result.Failure<EnrollmentStatusResponse>(EnrollmentErrors.CourseNotFound);

        var course = await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted, ct);

        if (course is null) return Result.Failure<EnrollmentStatusResponse>(EnrollmentErrors.CourseNotFound);

        ApplicationUser? user = null;
        if (!string.IsNullOrEmpty(request.UserId))
            user = await userManager.FindByIdAsync(request.UserId);

        CourseUser? courseUser = null;
        if (!string.IsNullOrEmpty(request.UserId))
        {
            courseUser = await context.CourseUsers
                .AsNoTracking()
                .Include(cu => cu.CourseRole)
                .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == request.UserId && !cu.IsDeleted, ct);
        }

        var isEnrolled = courseUser is not null;
        var canEnroll = !isEnrolled && course.Status != CourseStatus.Pending;
        string? cannotEnrollReason = null;

        if (!canEnroll && !isEnrolled)
            cannotEnrollReason = "This course is not currently available";

        if (user is not null && !user.EmailConfirmed && !isEnrolled)
        {
            canEnroll = false;
            cannotEnrollReason = "Please verify your email before enrolling";
        }

        // Check for pending payment (only for paid courses with non-enrolled users)
        var requiresPayment = !isEnrolled && course.Price > 0 && canEnroll;
        var hasPendingPayment = false;

        if (requiresPayment && !string.IsNullOrEmpty(request.UserId))
        {
            hasPendingPayment = await context.Payments
                .AnyAsync(p => p.CourseId == courseId
                               && p.UserId == request.UserId
                               && p.Status == PaymentStatus.Pending, ct);
        }

        return Result.Success(new EnrollmentStatusResponse
        {
            IsEnrolled = isEnrolled,
            CanEnroll = canEnroll,
            CannotEnrollReason = cannotEnrollReason,
            CourseId = request.CourseIdKey,
            CourseName = course.Title,
            IsFree = course.Price == 0,
            Price = course.Price,
            Currency = "USD",
            RequiresPayment = requiresPayment,
            HasPendingPayment = hasPendingPayment,
            CurrentRole = courseUser is not null ? (CourseRoleType)courseUser.CourseRole.Level : null,
            CurrentRoleName = courseUser?.CourseRole.Name,
            EnrolledOn = courseUser?.EnrolledOn
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

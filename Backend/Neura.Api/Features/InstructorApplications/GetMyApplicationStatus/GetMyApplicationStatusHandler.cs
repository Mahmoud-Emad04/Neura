using MediatR;
using Microsoft.AspNetCore.Identity;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.InstructorApplication;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.InstructorApplications.GetMyApplicationStatus;

internal sealed class GetMyApplicationStatusHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetMyApplicationStatusQuery, Result<MyApplicationStatusResponse>>
{
    public async Task<Result<MyApplicationStatusResponse>> Handle(
        GetMyApplicationStatusQuery query, CancellationToken ct)
    {
        var userId = query.UserId;
        var user = await userManager.FindByIdAsync(userId);

        if (user is null) return Result.Failure<MyApplicationStatusResponse>(InstructorApplicationErrors.UserNotFound);

        var isInstructor = await userManager.IsInRoleAsync(user, DefaultRoles.Instructor);

        if (isInstructor)
        {
            return Result.Success(new MyApplicationStatusResponse
            {
                HasApplication = false,
                IsInstructor = true,
                CanApply = false,
                Message = "You are already an approved instructor"
            });
        }

        var latestApplication = await context.InstructorApplications
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedOn)
            .FirstOrDefaultAsync(ct);

        if (latestApplication is null)
        {
            return Result.Success(new MyApplicationStatusResponse
            {
                HasApplication = false,
                IsInstructor = false,
                CanApply = true,
                Message = "You can apply to become an instructor"
            });
        }

        var canApply = latestApplication.Status == ApplicationStatus.Rejected &&
                       (!latestApplication.CanReapplyAfter.HasValue ||
                        DateTime.UtcNow >= latestApplication.CanReapplyAfter.Value);

        return Result.Success(new MyApplicationStatusResponse
        {
            HasApplication = true,
            IsInstructor = false,
            CanApply = canApply,
            ApplicationId = latestApplication.Id,
            Status = latestApplication.Status,
            RejectionReason = latestApplication.RejectionReason,
            Bio = latestApplication.Bio,
            Experience = latestApplication.Experience,
            CreatedOn = latestApplication.CreatedOn,
            ReviewedOn = latestApplication.ReviewedOn,
            CanReapplyAfter = latestApplication.CanReapplyAfter,
            Message = InstructorApplicationHelpers.GetStatusMessage(latestApplication.Status, latestApplication.CanReapplyAfter)
        });
    }
}

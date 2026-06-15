using MediatR;
using Microsoft.AspNetCore.Identity;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.InstructorApplication;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.InstructorApplications.ApproveApplication;

internal sealed class ApproveApplicationHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<ApproveApplicationHandler> logger)
    : IRequestHandler<ApproveApplicationCommand, Result<ApplicationResponse>>
{
    public async Task<Result<ApplicationResponse>> Handle(
        ApproveApplicationCommand command, CancellationToken ct)
    {
        var application = await context.InstructorApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == command.Id, ct);

        if (application is null)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationNotFound);

        if (application.Status != ApplicationStatus.Pending)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationAlreadyReviewed);

        var reviewer = await userManager.FindByIdAsync(command.ReviewerId);

        application.Status = ApplicationStatus.Approved;
        application.ReviewedById = command.ReviewerId;
        application.ReviewedOn = DateTime.UtcNow;

        var user = application.User;
        var roleResult = await userManager.AddToRoleAsync(user, DefaultRoles.Instructor);

        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to add Instructor role to user {UserId}: {Errors}",
                user.Id, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

            return Result.Failure<ApplicationResponse>(
                new Error("InstructorApplication.RoleAssignmentFailed",
                    "Failed to assign instructor role",
                    StatusCodes.Status409Conflict));
        }

        user.InstructorApprovedOn = DateTime.UtcNow;
        user.Bio = application.Bio;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Application {ApplicationId} approved by {ReviewerId}. User {UserId} is now an instructor",
            command.Id, command.ReviewerId, user.Id);

        return Result.Success(InstructorApplicationHelpers.MapToResponse(application, user, reviewer));
    }
}

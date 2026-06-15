using MediatR;
using Microsoft.AspNetCore.Identity;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.InstructorApplication;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.InstructorApplications.SubmitApplication;

internal sealed class SubmitApplicationHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<SubmitApplicationHandler> logger)
    : IRequestHandler<SubmitApplicationCommand, Result<ApplicationResponse>>
{
    public async Task<Result<ApplicationResponse>> Handle(
        SubmitApplicationCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var userId = command.UserId;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.UserNotFound);

        if (await userManager.IsInRoleAsync(user, DefaultRoles.Instructor))
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.AlreadyInstructor);

        var existingApplication = await context.InstructorApplications
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedOn)
            .FirstOrDefaultAsync(ct);

        if (existingApplication is not null)
        {
            if (existingApplication.Status == ApplicationStatus.Pending)
                return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.PendingApplicationExists);

            if (existingApplication.Status == ApplicationStatus.Rejected &&
                existingApplication.CanReapplyAfter.HasValue &&
                DateTime.UtcNow < existingApplication.CanReapplyAfter.Value)
                return Result.Failure<ApplicationResponse>(
                    InstructorApplicationErrors.ReapplyDateNotReached(existingApplication.CanReapplyAfter.Value));
        }

        var application = new InstructorApplication
        {
            UserId = userId,
            Bio = request.Bio.Trim(),
            Experience = request.Experience.Trim(),
            Status = ApplicationStatus.Pending,
            CreatedOn = DateTime.UtcNow
        };

        context.InstructorApplications.Add(application);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} submitted instructor application {ApplicationId}",
            userId, application.Id);

        return Result.Success(InstructorApplicationHelpers.MapToResponse(application, user));
    }
}

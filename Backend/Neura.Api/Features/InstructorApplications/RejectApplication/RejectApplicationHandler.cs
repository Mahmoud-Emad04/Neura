using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.InstructorApplication;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.InstructorApplications.RejectApplication;

internal sealed class RejectApplicationHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<RejectApplicationHandler> logger) 
    : IRequestHandler<RejectApplicationCommand, Result<ApplicationResponse>>
{
    public async Task<Result<ApplicationResponse>> Handle(
        RejectApplicationCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.RejectionReason))
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.RejectionReasonRequired);

        var application = await context.InstructorApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == command.Id, ct);

        if (application is null)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationNotFound);

        if (application.Status != ApplicationStatus.Pending)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationAlreadyReviewed);

        var reviewer = await userManager.FindByIdAsync(command.ReviewerId);

        application.Status = ApplicationStatus.Rejected;
        application.ReviewedById = command.ReviewerId;
        application.ReviewedOn = DateTime.UtcNow;
        application.RejectionReason = request.RejectionReason.Trim();
        application.CanReapplyAfter = DateTime.UtcNow.AddDays(CourseLimits.ReapplyWaitDays);

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Application {ApplicationId} rejected by {ReviewerId}",
            command.Id, command.ReviewerId);

        return Result.Success(InstructorApplicationHelpers.MapToResponse(application, application.User, reviewer));
    }
}

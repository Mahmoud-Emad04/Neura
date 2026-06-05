using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Core.InstructorApplication;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.InstructorApplications.UpdateApplication;

internal sealed class UpdateApplicationHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<UpdateApplicationHandler> logger) 
    : IRequestHandler<UpdateApplicationCommand, Result<ApplicationResponse>>
{
    public async Task<Result<ApplicationResponse>> Handle(
        UpdateApplicationCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var userId = command.UserId;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.UserNotFound);

        var application = await context.InstructorApplications
            .Where(a => a.UserId == userId && a.Status == ApplicationStatus.Pending)
            .FirstOrDefaultAsync(ct);

        if (application is null)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationNotFound);

        application.Bio = request.Bio.Trim();
        application.Experience = request.Experience.Trim();

        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} updated application {ApplicationId}", userId, application.Id);

        return Result.Success(InstructorApplicationHelpers.MapToResponse(application, user));
    }
}

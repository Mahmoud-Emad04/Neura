using Neura.Core.Entities;
using Neura.Core.Enums;
using Neura.Core.InstructorApplication;

namespace Neura.Api.Features.InstructorApplications;

public static class InstructorApplicationHelpers
{
    public static ApplicationResponse MapToResponse(
        InstructorApplication application,
        ApplicationUser user,
        ApplicationUser? reviewer = null)
    {
        return new ApplicationResponse
        {
            Id = application.Id,
            UserId = application.UserId,
            UserName = $"{user.FirstName} {user.LastName}",
            UserEmail = user.Email ?? string.Empty,
            Status = application.Status,
            Bio = application.Bio,
            Experience = application.Experience,
            RejectionReason = application.RejectionReason,
            CreatedOn = application.CreatedOn,
            ReviewedOn = application.ReviewedOn,
            ReviewedByName = reviewer is not null ? $"{reviewer.FirstName} {reviewer.LastName}" : null,
            CanReapplyAfter = application.CanReapplyAfter
        };
    }

    public static string GetStatusMessage(ApplicationStatus status, DateTime? canReapplyAfter)
    {
        return status switch
        {
            ApplicationStatus.Pending => "Your application is under review",
            ApplicationStatus.Approved => "Congratulations! Your application has been approved",
            ApplicationStatus.Rejected when canReapplyAfter.HasValue && DateTime.UtcNow < canReapplyAfter.Value =>
                $"Your application was rejected. You can reapply after {canReapplyAfter.Value:yyyy-MM-dd}",
            ApplicationStatus.Rejected => "Your application was rejected. You can submit a new application",
            _ => string.Empty
        };
    }
}

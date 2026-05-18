using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Api.Features.CourseTeam;

public static class CourseTeamHelpers
{
    public static TeamMemberResponse MapToTeamMemberResponse(CourseUser cu)
    {
        return new TeamMemberResponse
        {
            UserId = cu.UserId,
            FirstName = cu.User.FirstName,
            LastName = cu.User.LastName,
            Email = cu.User.Email ?? string.Empty,
            CourseRoleId = cu.CourseRoleId,
            RoleName = cu.CourseRole.Name,
            RoleType = (CourseRoleType)cu.CourseRole.Level,
            RoleLevel = cu.CourseRole.Level,
            PermissionMask = cu.PermissionMask,
            EnrolledOn = cu.EnrolledOn,
            EnrolledByName = cu.EnrolledBy is not null
                ? $"{cu.EnrolledBy.FirstName} {cu.EnrolledBy.LastName}"
                : null,
            LastAccessedOn = cu.LastAccessedOn,
            IsOwner = cu.CourseRole.Level == (int)CourseRoleType.CourseOwner
        };
    }

    public static CourseInvitationResponse MapToInvitationResponse(CourseInvitation ci)
    {
        return new CourseInvitationResponse
        {
            Id = ci.Id,
            CourseId = ci.CourseId,
            CourseName = ci.Course.Title,
            Email = ci.Email,
            RoleName = ci.CourseRole.Name,
            RoleType = (CourseRoleType)ci.CourseRole.Level,
            Status = ci.Status,
            CustomMessage = ci.CustomMessage,
            InvitedByName = $"{ci.InvitedBy.FirstName} {ci.InvitedBy.LastName}",
            InvitedOn = ci.InvitedOn,
            ExpiresOn = ci.ExpiresOn,
            RespondedOn = ci.RespondedOn
        };
    }

    public static string GenerateInvitationToken()
    {
        return $"{Guid.NewGuid():N}{Guid.NewGuid():N}"[..64];
    }

    public static string? GetInvalidReason(CourseInvitation invitation)
    {
        if (invitation.Status == InvitationStatus.Accepted)
            return "This invitation has already been accepted";

        if (invitation.Status == InvitationStatus.Rejected)
            return "This invitation has been rejected";

        if (invitation.Status == InvitationStatus.Cancelled)
            return "This invitation has been cancelled";

        if (invitation.Status == InvitationStatus.Expired || DateTime.UtcNow >= invitation.ExpiresOn)
            return "This invitation has expired";

        return null;
    }
}

using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class CourseTeamErrors
{
    public static readonly Error CourseNotFound =
        new("CourseTeam.CourseNotFound", "Course not found", StatusCodes.Status404NotFound);

    public static readonly Error UserNotFound =
        new("CourseTeam.UserNotFound", "User not found", StatusCodes.Status404NotFound);

    public static readonly Error MemberNotFound =
        new("CourseTeam.MemberNotFound", "Team member not found", StatusCodes.Status404NotFound);

    public static readonly Error InvitationNotFound =
        new("CourseTeam.InvitationNotFound", "Invitation not found", StatusCodes.Status404NotFound);

    public static readonly Error InvitationExpired =
        new("CourseTeam.InvitationExpired", "This invitation has expired", StatusCodes.Status400BadRequest);

    public static readonly Error InvitationAlreadyResponded =
        new("CourseTeam.InvitationAlreadyResponded", "This invitation has already been responded to",
            StatusCodes.Status409Conflict);

    public static readonly Error AlreadyTeamMember =
        new("CourseTeam.AlreadyTeamMember", "User is already a team member of this course",
            StatusCodes.Status409Conflict);

    public static readonly Error AlreadyInvited =
        new("CourseTeam.AlreadyInvited", "A pending invitation already exists for this email",
            StatusCodes.Status409Conflict);

    public static readonly Error CannotInviteSelf =
        new("CourseTeam.CannotInviteSelf", "You cannot invite yourself", StatusCodes.Status400BadRequest);

    public static readonly Error CannotRemoveSelf =
        new("CourseTeam.CannotRemoveSelf", "You cannot remove yourself from the team", StatusCodes.Status400BadRequest);

    public static readonly Error CannotRemoveOwner =
        new("CourseTeam.CannotRemoveOwner", "Cannot remove the course owner", StatusCodes.Status400BadRequest);

    public static readonly Error CannotChangeOwnerRole =
        new("CourseTeam.CannotChangeOwnerRole", "Cannot change the course owner's role. Use transfer ownership instead",
            StatusCodes.Status400BadRequest);

    public static readonly Error InsufficientPermission =
        new("CourseTeam.InsufficientPermission", "You don't have permission to perform this action",
            StatusCodes.Status403Forbidden);

    public static readonly Error CannotManageHigherRole =
        new("CourseTeam.CannotManageHigherRole", "You cannot manage users with equal or higher role",
            StatusCodes.Status403Forbidden);

    public static readonly Error InvalidRole =
        new("CourseTeam.InvalidRole", "Invalid role specified", StatusCodes.Status400BadRequest);

    public static readonly Error CannotAssignOwnerRole =
        new("CourseTeam.CannotAssignOwnerRole", "Cannot assign owner role. Use transfer ownership instead",
            StatusCodes.Status400BadRequest);

    public static readonly Error TransferToSelf =
        new("CourseTeam.TransferToSelf", "Cannot transfer ownership to yourself", StatusCodes.Status400BadRequest);

    public static readonly Error TransferTargetNotTeamMember =
        new("CourseTeam.TransferTargetNotTeamMember", "Transfer target must be an existing team member",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidToken =
        new("CourseTeam.InvalidToken", "Invalid or expired invitation token", StatusCodes.Status400BadRequest);

    public static Error TeamLimitReached(int maxMembers)
    {
        return new Error("CourseTeam.TeamLimitReached",
            $"Team member limit reached. Maximum {maxMembers} members allowed",
            StatusCodes.Status400BadRequest);
    }
}
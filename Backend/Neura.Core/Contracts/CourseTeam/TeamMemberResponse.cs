using Neura.Core.Enums;

namespace Neura.Core.Contracts.CourseTeam;

public class TeamMemberResponse
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public int CourseRoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public CourseRoleType RoleType { get; set; }
    public int RoleLevel { get; set; }
    public int PermissionMask { get; set; }
    public DateTime EnrolledOn { get; set; }
    public string? EnrolledByName { get; set; }
    public DateTime? LastAccessedOn { get; set; }
    public bool IsOwner { get; set; }
}
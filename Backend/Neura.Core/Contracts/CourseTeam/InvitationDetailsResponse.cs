using Neura.Core.Enums;

namespace Neura.Core.Contracts.CourseTeam;

public class InvitationDetailsResponse
{
    public int InvitationId { get; set; }
    public string Token { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseDescription { get; set; } = string.Empty;
    public string? CourseThumbnail { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
    public CourseRoleType RoleType { get; set; }
    public string? CustomMessage { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
    public string InvitedByEmail { get; set; } = string.Empty;
    public DateTime InvitedOn { get; set; }
    public DateTime ExpiresOn { get; set; }
    public InvitationStatus Status { get; set; }
    public bool IsValid { get; set; }
    public string? InvalidReason { get; set; }
}
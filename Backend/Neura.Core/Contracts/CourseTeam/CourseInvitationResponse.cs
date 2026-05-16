using Neura.Core.Enums;

namespace Neura.Core.Contracts.CourseTeam;

public class CourseInvitationResponse
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public CourseRoleType RoleType { get; set; }
    public InvitationStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public string? CustomMessage { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
    public DateTime InvitedOn { get; set; }
    public DateTime ExpiresOn { get; set; }
    public DateTime? RespondedOn { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
    public bool CanRespond => Status == InvitationStatus.Pending && !IsExpired;
}
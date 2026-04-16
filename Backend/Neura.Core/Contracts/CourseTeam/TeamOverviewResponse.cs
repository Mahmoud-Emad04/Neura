namespace Neura.Core.Contracts.CourseTeam;

public class TeamOverviewResponse
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int TotalMembers { get; set; }
    public int MaxMembers { get; set; }
    public bool CanInviteMore { get; set; }
    public List<TeamMemberResponse> Members { get; set; } = [];
    public List<CourseInvitationResponse> PendingInvitations { get; set; } = [];
}
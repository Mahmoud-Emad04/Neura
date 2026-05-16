namespace Neura.Core.Contracts.CourseTeam;

public class MyInvitationsResponse
{
    public List<CourseInvitationResponse> PendingInvitations { get; set; } = [];
    public int TotalPending { get; set; }
}
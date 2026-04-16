using Neura.Core.Abstractions;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Core.Services;

public interface ICourseTeamService
{
    // Team Management
    Task<Result<TeamOverviewResponse>> GetTeamOverviewAsync(int courseId, string requesterId);
    Task<Result<List<TeamMemberResponse>>> GetTeamMembersAsync(int courseId);
    Task<Result<TeamMemberResponse>> GetTeamMemberAsync(int courseId, string userId);
    Task<Result> RemoveTeamMemberAsync(int courseId, string userId, string requesterId);

    Task<Result<TeamMemberResponse>> ChangeTeamRoleAsync(int courseId, string userId, ChangeTeamRoleRequest request,
        string requesterId);

    Task<Result> TransferOwnershipAsync(int courseId, TransferOwnershipRequest request, string requesterId);

    // Invitations - Course Owner Side
    Task<Result<CourseInvitationResponse>> InviteTeamMemberAsync(int courseId, InviteTeamMemberRequest request,
        string inviterId);

    Task<Result<List<CourseInvitationResponse>>> GetPendingInvitationsAsync(int courseId);
    Task<Result> CancelInvitationAsync(int courseId, int invitationId, string requesterId);
    Task<Result<CourseInvitationResponse>> ResendInvitationAsync(int courseId, int invitationId, string requesterId);

    // Invitations - Invitee Side
    Task<Result<InvitationDetailsResponse>> GetInvitationByTokenAsync(string token);
    Task<Result<TeamMemberResponse>> AcceptInvitationAsync(string token, string userId);
    Task<Result> RejectInvitationAsync(string token, string? userId = null);
    Task<Result<MyInvitationsResponse>> GetMyInvitationsAsync(string userEmail);
}
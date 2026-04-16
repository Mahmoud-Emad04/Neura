using Neura.Core.Enums;

namespace Neura.Core.Entities;

/// <summary>
///     Tracks team member invitations for courses
/// </summary>
public class CourseInvitation
{
    public int Id { get; set; }

    /// <summary>
    ///     Course the invitation is for
    /// </summary>
    public int CourseId { get; set; }

    public Course Course { get; set; } = default!;

    /// <summary>
    ///     Email address of the invitee
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    ///     Unique token for accepting/rejecting
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    ///     Role being offered
    /// </summary>
    public int CourseRoleId { get; set; }

    public CourseRole CourseRole { get; set; } = default!;

    /// <summary>
    ///     Current status of invitation
    /// </summary>
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// <summary>
    ///     Optional message from inviter
    /// </summary>
    public string? CustomMessage { get; set; }

    /// <summary>
    ///     User who sent the invitation
    /// </summary>
    public string InvitedById { get; set; } = string.Empty;

    public ApplicationUser InvitedBy { get; set; } = default!;

    /// <summary>
    ///     When invitation was sent
    /// </summary>
    public DateTime InvitedOn { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     When invitation expires
    /// </summary>
    public DateTime ExpiresOn { get; set; }

    /// <summary>
    ///     When invitee responded (if they did)
    /// </summary>
    public DateTime? RespondedOn { get; set; }

    /// <summary>
    ///     User who accepted (if they registered/accepted)
    /// </summary>
    public string? AcceptedUserId { get; set; }

    public ApplicationUser? AcceptedUser { get; set; }

    /// <summary>
    ///     Check if invitation is still valid
    /// </summary>
    public bool IsValid => Status == InvitationStatus.Pending
                           && DateTime.UtcNow < ExpiresOn;
}
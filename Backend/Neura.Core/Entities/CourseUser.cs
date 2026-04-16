namespace Neura.Core.Entities;

/// <summary>
///     Junction table linking users to courses with role-based permissions
/// </summary>
public class CourseUser
{
    /// <summary>
    ///     Course ID (composite PK part 1)
    /// </summary>
    public int CourseId { get; set; }

    public Course Course { get; set; } = default!;

    /// <summary>
    ///     User ID (composite PK part 2)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = default!;

    /// <summary>
    ///     Role in this course (Student, Assistant, etc.)
    /// </summary>
    public int CourseRoleId { get; set; }

    public CourseRole CourseRole { get; set; } = default!;

    /// <summary>
    ///     Bitwise permission mask (copied from role, can be customized)
    /// </summary>
    public int PermissionMask { get; set; }

    /// <summary>
    ///     When user was enrolled/added
    /// </summary>
    public DateTime EnrolledOn { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Who added this user (null if self-enrolled)
    /// </summary>
    public string? EnrolledById { get; set; }

    public ApplicationUser? EnrolledBy { get; set; }

    /// <summary>
    ///     Invitation that led to this membership (if any)
    /// </summary>
    public int? InvitationId { get; set; }

    public CourseInvitation? Invitation { get; set; }

    /// <summary>
    ///     Last time user accessed the course
    /// </summary>
    public DateTime? LastAccessedOn { get; set; }

    /// <summary>
    ///     Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }
}
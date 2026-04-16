using Neura.Core.Enums;

namespace Neura.Core.Entities;

/// <summary>
///     Tracks instructor role applications from members
/// </summary>
public class InstructorApplication
{
    public int Id { get; set; }

    /// <summary>
    ///     User who submitted the application
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = default!;

    /// <summary>
    ///     Current status of the application
    /// </summary>
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    /// <summary>
    ///     Applicant's biography/background
    /// </summary>
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    ///     Applicant's relevant experience
    /// </summary>
    public string Experience { get; set; } = string.Empty;

    /// <summary>
    ///     Reason for rejection (if rejected)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    ///     Admin who reviewed the application
    /// </summary>
    public string? ReviewedById { get; set; }

    public ApplicationUser? ReviewedBy { get; set; }

    /// <summary>
    ///     When the application was reviewed
    /// </summary>
    public DateTime? ReviewedOn { get; set; }

    /// <summary>
    ///     When the application was submitted
    /// </summary>
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     If rejected, earliest date user can reapply
    /// </summary>
    public DateTime? CanReapplyAfter { get; set; }
}
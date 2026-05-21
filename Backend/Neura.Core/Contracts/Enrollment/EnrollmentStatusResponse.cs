using Neura.Core.Enums;

namespace Neura.Core.Contracts.Enrollment;

public class EnrollmentStatusResponse
{
    public bool IsEnrolled { get; set; }
    public bool CanEnroll { get; set; }
    public string? CannotEnrollReason { get; set; }
    public string CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }

    /// <summary>
    ///     True when the course has a price > 0 and the user is not yet enrolled.
    ///     Frontend should redirect to the checkout endpoint when this is true.
    /// </summary>
    public bool RequiresPayment { get; set; }

    /// <summary>
    ///     True when the user already has a pending Stripe checkout session for this course.
    /// </summary>
    public bool HasPendingPayment { get; set; }

    public CourseRoleType? CurrentRole { get; set; }
    public string? CurrentRoleName { get; set; }
    public DateTime? EnrolledOn { get; set; }
}
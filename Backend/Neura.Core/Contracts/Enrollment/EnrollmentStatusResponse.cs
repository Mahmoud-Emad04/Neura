using Neura.Core.Enums;

namespace Neura.Core.Contracts.Enrollment;

public class EnrollmentStatusResponse
{
    public bool IsEnrolled { get; set; }
    public bool CanEnroll { get; set; }
    public string? CannotEnrollReason { get; set; }
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public CourseRoleType? CurrentRole { get; set; }
    public string? CurrentRoleName { get; set; }
    public DateTime? EnrolledOn { get; set; }
}
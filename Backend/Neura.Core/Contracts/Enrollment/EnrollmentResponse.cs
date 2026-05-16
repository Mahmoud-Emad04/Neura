using Neura.Core.Enums;

namespace Neura.Core.Contracts.Enrollment;

public class EnrollmentResponse
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? CourseThumbnail { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public CourseRoleType Role { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime EnrolledOn { get; set; }
    public DateTime? LastAccessedOn { get; set; }
    public bool IsActive { get; set; }
}
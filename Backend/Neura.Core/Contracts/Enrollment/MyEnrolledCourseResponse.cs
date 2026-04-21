using Neura.Core.Enums;

namespace Neura.Core.Contracts.Enrollment;

public class MyEnrolledCourseResponse
{
    public string CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? CourseDescription { get; set; }
    public string? CourseThumbnail { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public CourseRoleType Role { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsTeamMember { get; set; }
    public bool IsOwner { get; set; }
    public DateTime EnrolledOn { get; set; }
    public DateTime? LastAccessedOn { get; set; }
    public int? ProgressPercentage { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
}
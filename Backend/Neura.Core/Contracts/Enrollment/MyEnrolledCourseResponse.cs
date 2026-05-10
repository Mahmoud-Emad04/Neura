using Neura.Core.Enums;

namespace Neura.Core.Contracts.Enrollment;

public class MyEnrolledCourseResponse
{
    public string KeyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CourseDescription { get; set; }
    public string? ImageUrl { get; set; }
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

    // Course metadata
    public int NumberOfLessons { get; set; }
    public double Hours { get; set; }
    public int Price { get; set; }
    public double Rating { get; set; }
    public bool IsBookmarked { get; set; }
}
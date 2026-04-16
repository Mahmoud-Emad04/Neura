namespace Neura.Core.Contracts.Enrollment;

public class CourseStudentResponse
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public DateTime EnrolledOn { get; set; }
    public DateTime? LastAccessedOn { get; set; }
    public int? ProgressPercentage { get; set; }
    public int CompletedLessons { get; set; }
}
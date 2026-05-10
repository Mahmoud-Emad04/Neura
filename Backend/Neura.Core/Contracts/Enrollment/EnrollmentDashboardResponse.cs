namespace Neura.Core.Contracts.Enrollment;

public class EnrollmentDashboardResponse
{
    public int TotalCourses { get; set; }
    public int CompletedCourses { get; set; }
    public int InProgressCourses { get; set; }
    public double TotalHours { get; set; }
}

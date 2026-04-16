namespace Neura.Core.Contracts.Enrollment;

public class CourseStudentsListResponse
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int? MaxStudents { get; set; }
    public List<CourseStudentResponse> Students { get; set; } = [];
}
using Neura.Core.Enums;

namespace Neura.Core.Contracts.Course;

public record CourseSummaryResponse
{
    public string KeyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Price { get; set; }
    public double Rating { get; set; }

    public int NumberOfStudents { get; set; }

    // Status
    public CourseStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsEnrollmentOpen { get; set; }

    public bool IsBookmarked { get; set; }
    public bool IsEnrolled { get; set; }

    public int TotalReviews { get; set; }
    public List<TagResponse> Tags { get; set; } = new();
}
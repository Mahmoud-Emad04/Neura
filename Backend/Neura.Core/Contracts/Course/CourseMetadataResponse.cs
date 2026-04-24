using Neura.Core.Enums;

namespace Neura.Core.Contracts.Course;

public sealed record CourseMetadataResponse
{
    public string KeyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Price { get; set; }
    public double Rating { get; set; }
    public int TotalReviews { get; set; }

    public CourseStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsEnrollmentOpen { get; set; }


    public int NumberOfStudents { get; set; }

    public bool IsEnrolled { get; set; }
    public bool IsBookmarked { get; set; }
    public bool IsOwner { get; set; }
    public bool IsPubliclyVisible { get; set; }

    public IEnumerable<string> Tags { get; set; } = [];
    public IEnumerable<string> LearningOutcomes { get; set; } = [];
    public IEnumerable<string> Prerequisites { get; set; } = [];
}
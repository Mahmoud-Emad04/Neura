using Neura.Core.Contracts.Instructor;
using Neura.Core.Contracts.Section;

namespace Neura.Core.Contracts.Course;

public record CourseResponse
{
    public string KeyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsEnrolled { get; set; }
    public bool IsOwner { get; set; }
    public int Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public double Rating { get; set; }
    public bool IsBookmarked { get; set; }
    public DateOnly Startin { get; set; }
    public DateOnly Endin { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedById { get; set; }
    public string CreatedById { get; set; } = string.Empty;

    public int TotalReviews { get; set; }
    public int NumberOfStudents { get; set; }
    public int Hours { get; set; }


    public InstructorSummaryResponse Instructor { get; set; } = default!;
    public List<SectionResponse>? Sections { get; set; } = [];
    public List<TagResponse> Tags { get; set; } = [];
    public List<string> LearningOutcomes { get; set; } = [];
    public List<string> Prerequisites { get; set; } = [];
}
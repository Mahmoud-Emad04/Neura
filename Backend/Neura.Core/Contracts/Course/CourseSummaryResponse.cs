namespace Neura.Core.Contracts.Course;

public record CourseSummaryResponse
{
    public string KeyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsEnrolled { get; set; }
    public int Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public double Rating { get; set; }
    public bool IsBookmarked { get; set; }
    public List<TagResponse> Tags { get; set; } = new();
}
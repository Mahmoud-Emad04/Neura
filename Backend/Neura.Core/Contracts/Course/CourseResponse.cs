namespace Neura.Core.Contracts.Course;

public record CourseResponse
{
    public string KeyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsEnrolled { get; set; }
    public int Price { get; set; }
    public double Rating { get; set; }
    public DateOnly Startin { get; set; }
    public DateOnly Endin { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string? UpdatedById { get; set; }
    public string CreatedById { get; set; } = string.Empty;

    public (int DifficultyId, string DifficultyName) Difficulty { get; set; }

    public List<TopicResponse>? Topics { get; set; }
    public List<TagResponse> Tags { get; set; } = new();
}
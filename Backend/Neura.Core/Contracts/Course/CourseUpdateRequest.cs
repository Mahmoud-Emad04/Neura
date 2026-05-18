namespace Neura.Core.Contracts.Course;

public record CourseUpdateRequest
{
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public int Price { get; init; }
    public IFormFile? Image { get; init; }
    public string InstructorName { get; init; } = default!;
    public List<int> Tags { get; init; } = [];
    public List<string> LearningOutcomes { get; init; } = [];
    public List<string> Prerequisites { get; init; } = [];
}
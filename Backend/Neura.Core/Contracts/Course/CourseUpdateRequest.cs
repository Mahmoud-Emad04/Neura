namespace Neura.Core.Contracts.Course;

public record CourseUpdateRequest(
    string Title,
    string Description,
    bool IsCompleted,
    DateOnly Startin,
    DateOnly Endin,
    List<int> Tags);
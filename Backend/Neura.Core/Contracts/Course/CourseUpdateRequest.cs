namespace Neura.Core.Contracts.Course;

public record CourseUpdateRequest(
    string Title,
    string Description,
    DateOnly Startin,
    DateOnly Endin,
    bool IsPubliclyVisible,
    List<int> Tags,
    List<string> LearningOutcomes,
    List<string> Prerequisites);
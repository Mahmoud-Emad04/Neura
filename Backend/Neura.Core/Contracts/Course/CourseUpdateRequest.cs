namespace Neura.Core.Contracts.Course;

public record CourseUpdateRequest(
    string Title,
    string Description,
    int Price,
    bool IsPubliclyVisible,
    IFormFile? Image,
    string InstructorName,
    List<int> Tags,
    List<string> LearningOutcomes,
    List<string> Prerequisites
);
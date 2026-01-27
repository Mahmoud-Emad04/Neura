namespace Neura.Core.Contracts.Course;

public record CourseRequest(
    string Title,
    string InstructorName,
    string Description,
    int DifficultyId,
    int Price,
    DateOnly Startin,
    DateOnly Endin,
    List<int> Tags
);
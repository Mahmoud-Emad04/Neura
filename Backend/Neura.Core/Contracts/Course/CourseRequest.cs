namespace Neura.Core.Contracts.Course;

public record CourseRequest(
    string Title,
    string Description,
    int Price,
    DateOnly Startin,
    DateOnly Endin,
    List<int> Tags
);
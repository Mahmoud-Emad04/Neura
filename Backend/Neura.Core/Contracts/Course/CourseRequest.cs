namespace Neura.Core.Contracts.Course;

public record CourseRequest(
    string Title,
    string Description,
    int Price,
    IFormFile? Image,
    DateOnly StartDate,
    DateOnly EndDate,
    List<int> Tags,
    List<string> LearningOutcomes,
    List<string> Prerequisites
);
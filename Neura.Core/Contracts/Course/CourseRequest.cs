namespace Neura.Core.Contracts.Course;

public record CourseRequest(
    string Title,
    string Description,
    List<TopicRequest>? Topics);
namespace Neura.Core.Contracts.Course;

public record CourseResponse(
    string KeyId,
    string Title,
    string Description,
    bool IsCompleted,
    DateOnly Startin,
    DateOnly Endin,
    DateTime CreatedOn,
    DateTime? UpdatedOn,
    string ImageUrl,
    string? UpdatedById,
    string CreatedById,
    List<TopicResponse>? Topics,
    List<TagResponse> Tags);

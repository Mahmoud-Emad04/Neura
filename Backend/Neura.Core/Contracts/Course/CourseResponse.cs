namespace Neura.Core.Contracts.Course;

public record CourseResponse(
    string KeyId,
    string Title,
    string InstructorName,
    string Description,
    bool IsCompleted,
    int Price,
    DateOnly Startin,
    DateOnly Endin,
    DateTime CreatedOn,
    DateTime? UpdatedOn,
    string ImageUrl,
    string? UpdatedById,
    string CreatedById,
    (int DifficultyId, string DifficultyName) Difficulty,
    List<TopicResponse>? Topics,
    List<TagResponse> Tags);

namespace GraduationProject.Core.Contracts.Course;

public record CourseResponse(
    string KeyId,
    string Title,
    bool IsCompleted,
    string Description,
    DateTime CreatedOn,
    DateTime? UpdatedById,
    string? LastUpdatedBy,
    string CreatedById,
    List<TopicResponse> Topics);

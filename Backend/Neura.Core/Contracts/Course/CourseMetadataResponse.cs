namespace Neura.Core.Contracts.Course;

public record CourseMetadataResponse(
        string KeyId,
        string Title,
        string Description,
        string ImageUrl,

        DateOnly Startin,
        DateOnly Endin,
        DateTime CreatedOn,
        DateTime? UpdatedOn,
        string? UpdatedById,
        string CreatedById,

        bool IsCompleted,
        bool IsEnrolled,
        bool IsOwner,
        bool IsBookmarked,

        int Price,
        double Rating,
        int TotalReviews,
        int NumberOfStudents,

        List<string>? LearningOutcomes,
        List<TagResponse> Tags,
        List<string>? Prerequisites
    );

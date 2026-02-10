namespace Neura.Core.Contracts.Instructor;

public record InstructorSummaryResponse(
    string Id,
    string Name,
    string? Headline,
    string? ImageUrl,
    int TotalStudents,
    double Rating,
    int TotalCourses,
    int TotalReviews
);
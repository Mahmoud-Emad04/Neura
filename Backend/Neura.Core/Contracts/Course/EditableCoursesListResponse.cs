using Neura.Core.Abstractions;

namespace Neura.Core.Contracts.Courses;

public sealed record EditableCoursesListResponse
{
    public int TotalOwnedCourses { get; init; }
    public int TotalCoInstructorCourses { get; init; }
    public PaginatedList<EditableCourseResponse> Courses { get; init; } = default!;
}

public sealed record EditableCoursesListSummaryResponse
{
    public int TotalOwnedCourses { get; init; }
    public int TotalCoInstructorCourses { get; init; }
    public PaginatedList<EditableCourseSummaryResponse> Courses { get; init; } = default!;
}
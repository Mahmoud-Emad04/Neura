using Neura.Core.Enums;

namespace Neura.Core.Contracts.Courses;

public sealed record EditableCourseResponse
{
    public string KeyId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public int Price { get; init; }
    public double Rating { get; init; }
    public int TotalReviews { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    // Status
    public CourseStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public bool IsEnrollmentOpen { get; init; }
    public bool IsPubliclyVisible { get; init; }

    // User's role in this course
    public string RoleName { get; init; } = string.Empty;
    public bool IsOwner { get; init; }
    public bool IsCoInstructor { get; init; }

    // Stats
    public int NumberOfStudents { get; init; }
    public int NumberOfSections { get; init; }
    public int NumberOfLessons { get; init; }
    public int PublishedLessons { get; init; }

    // Available actions based on role and status
    public CourseAvailableActions AvailableActions { get; init; } = new();

    // Timestamps
    public DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }
}
public sealed record EditableCourseSummaryResponse
{
    public string KeyId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;

    // Status
    public CourseStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public bool IsEnrollmentOpen { get; init; }
    public bool IsPubliclyVisible { get; init; }

    // User's role in this course
    public string RoleName { get; init; } = string.Empty;
    public bool IsOwner { get; init; }
    public bool IsCoInstructor { get; init; }

    // Stats
    public int NumberOfStudents { get; init; }

    public CourseAvailableActions AvailableActions { get; init; } = new();

    // Timestamps
    public DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }
}
public sealed record CourseAvailableActions
{
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
    public bool CanActivate { get; init; }
    public bool CanComplete { get; init; }
    public bool CanReactivate { get; init; }
    public bool CanUnpublish { get; init; }
    public bool CanManageStudents { get; init; }
    public bool CanManageInstructors { get; init; }
    public bool CanAddSections { get; init; }
    public bool CanAddLessons { get; init; }
}
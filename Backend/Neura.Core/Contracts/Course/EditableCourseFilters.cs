using Neura.Core.Enums;

namespace Neura.Core.Contracts.Courses;

public sealed record EditableCourseFilters
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    /// <summary>
    ///     Search by title
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    ///     Filter by status (Pending, Active, Completed)
    /// </summary>
    public CourseStatus? Status { get; init; }

    /// <summary>
    ///     Filter by role (Owner, CoInstructor, or All)
    /// </summary>
    public EditableRoleFilter RoleFilter { get; init; } = EditableRoleFilter.All;

    /// <summary>
    ///     Sort field: Title, CreatedOn, UpdatedOn, Status, Students
    /// </summary>
    public string SortBy { get; init; } = "UpdatedOn";

    /// <summary>
    ///     Sort direction
    /// </summary>
    public bool SortDescending { get; init; } = true;
}
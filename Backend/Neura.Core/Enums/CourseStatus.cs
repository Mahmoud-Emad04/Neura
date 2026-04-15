namespace Neura.Core.Enums;

/// <summary>
///     Represents the lifecycle status of a course.
/// </summary>
public enum CourseStatus
{
    /// <summary>
    ///     Course is being built by instructor, not visible to students.
    /// </summary>
    Pending = 1,

    /// <summary>
    ///     Course is published and available for enrollment.
    /// </summary>
    Active = 2,

    /// <summary>
    ///     Course has ended. Enrolled students can still access content, but no new enrollments.
    /// </summary>
    Completed = 3
}
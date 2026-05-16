namespace Neura.Core.Enums;

/// <summary>
///     Predefined course role types with hierarchical levels
/// </summary>
public enum CourseRoleType
{
    /// <summary>
    ///     Can only view content (Level 1)
    /// </summary>
    Student = 1,

    /// <summary>
    ///     Can view analytics and help students (Level 2)
    /// </summary>
    Assistant = 2,

    /// <summary>
    ///     Can edit content (Level 3)
    /// </summary>
    CoInstructor = 3,

    /// <summary>
    ///     Full control over course (Level 4)
    /// </summary>
    CourseOwner = 4
}
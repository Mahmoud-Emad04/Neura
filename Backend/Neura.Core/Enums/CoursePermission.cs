namespace Neura.Core.Enums;

/// <summary>
///     Bitwise flags for course-level permissions
///     Use [Flags] to combine multiple permissions
/// </summary>
[Flags]
public enum CoursePermission
{
    None = 0,

    /// <summary>
    ///     View lessons and materials (Bit 0)
    /// </summary>
    ViewContent = 1 << 0, // 1

    /// <summary>
    ///     View student progress and analytics (Bit 1)
    /// </summary>
    ViewAnalytics = 1 << 1, // 2

    /// <summary>
    ///     Answer questions and moderate discussions (Bit 2)
    /// </summary>
    ManageQA = 1 << 2, // 4

    /// <summary>
    ///     Create/edit sections and lessons (Bit 3)
    /// </summary>
    EditContent = 1 << 3, // 8

    /// <summary>
    ///     Add/remove students (Bit 4)
    /// </summary>
    ManageStudents = 1 << 4, // 16

    /// <summary>
    ///     Invite/remove team members (Bit 5)
    /// </summary>
    ManageTeam = 1 << 5, // 32

    /// <summary>
    ///     Edit course settings (Bit 6)
    /// </summary>
    ManageSettings = 1 << 6, // 64

    /// <summary>
    ///     Delete the course (Bit 7)
    /// </summary>
    DeleteCourse = 1 << 7, // 128

    /// <summary>
    ///     Transfer ownership to another user (Bit 8)
    /// </summary>
    TransferOwnership = 1 << 8 // 256
}
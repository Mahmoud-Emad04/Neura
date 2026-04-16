using Neura.Core.Enums;

namespace Neura.Core.Abstractions.Consts;

/// <summary>
///     Predefined permission masks for each course role
/// </summary>
public static class CoursePermissionMasks
{
    /// <summary>
    ///     Student: View content only (1)
    /// </summary>
    public const int Student = (int)CoursePermission.ViewContent;

    /// <summary>
    ///     Assistant: View + Analytics + QA (7)
    /// </summary>
    public const int Assistant =
        (int)CoursePermission.ViewContent |
        (int)CoursePermission.ViewAnalytics |
        (int)CoursePermission.ManageQA;

    /// <summary>
    ///     CoInstructor: Assistant + Edit Content (15)
    /// </summary>
    public const int CoInstructor =
        Assistant |
        (int)CoursePermission.EditContent;

    /// <summary>
    ///     CourseOwner: All permissions (511)
    /// </summary>
    public const int CourseOwner =
        (int)CoursePermission.ViewContent |
        (int)CoursePermission.ViewAnalytics |
        (int)CoursePermission.ManageQA |
        (int)CoursePermission.EditContent |
        (int)CoursePermission.ManageStudents |
        (int)CoursePermission.ManageTeam |
        (int)CoursePermission.ManageSettings |
        (int)CoursePermission.DeleteCourse |
        (int)CoursePermission.TransferOwnership;

    /// <summary>
    ///     Get permission mask for a role type
    /// </summary>
    public static int GetMaskForRole(CourseRoleType role)
    {
        return role switch
        {
            CourseRoleType.Student => Student,
            CourseRoleType.Assistant => Assistant,
            CourseRoleType.CoInstructor => CoInstructor,
            CourseRoleType.CourseOwner => CourseOwner,
            _ => 0
        };
    }

    /// <summary>
    ///     Check if a mask has a specific permission
    /// </summary>
    public static bool HasPermission(int mask, CoursePermission permission)
    {
        return (mask & (int)permission) == (int)permission;
    }
}
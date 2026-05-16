using Neura.Core.Abstractions.Consts;
using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Core.Extensions;

public static class PermissionExtensions
{
    /// <summary>
    ///     Check if user has a specific permission in course
    /// </summary>
    public static bool HasPermission(this CourseUser courseUser, CoursePermission permission)
    {
        return CoursePermissionMasks.HasPermission(courseUser.PermissionMask, permission);
    }

    /// <summary>
    ///     Check if user has all specified permissions
    /// </summary>
    public static bool HasAllPermissions(this CourseUser courseUser, params CoursePermission[] permissions)
    {
        foreach (var permission in permissions)
            if (!courseUser.HasPermission(permission))
                return false;
        return true;
    }

    /// <summary>
    ///     Check if user has any of the specified permissions
    /// </summary>
    public static bool HasAnyPermission(this CourseUser courseUser, params CoursePermission[] permissions)
    {
        foreach (var permission in permissions)
            if (courseUser.HasPermission(permission))
                return true;
        return false;
    }

    /// <summary>
    ///     Check if user can manage another user based on role hierarchy
    /// </summary>
    public static bool CanManage(this CourseUser manager, CourseUser target)
    {
        // Owner can manage anyone except other owners
        if (manager.CourseRole.Level == (int)CourseRoleType.CourseOwner)
            return target.CourseRole.Level < (int)CourseRoleType.CourseOwner;

        // Others can only manage users with lower level
        return manager.CourseRole.Level > target.CourseRole.Level;
    }

    /// <summary>
    ///     Get list of permissions from mask
    /// </summary>
    public static List<CoursePermission> GetPermissions(this CourseUser courseUser)
    {
        var permissions = new List<CoursePermission>();

        foreach (var permission in Enum.GetValues<CoursePermission>())
        {
            if (permission == CoursePermission.None)
                continue;

            if (courseUser.HasPermission(permission))
                permissions.Add(permission);
        }

        return permissions;
    }

    /// <summary>
    ///     Check if this is a team member (not a student)
    /// </summary>
    public static bool IsTeamMember(this CourseUser courseUser)
    {
        return courseUser.CourseRole.Level >= (int)CourseRoleType.Assistant;
    }

    /// <summary>
    ///     Check if this is the course owner
    /// </summary>
    public static bool IsOwner(this CourseUser courseUser)
    {
        return courseUser.CourseRole.Level == (int)CourseRoleType.CourseOwner;
    }
}
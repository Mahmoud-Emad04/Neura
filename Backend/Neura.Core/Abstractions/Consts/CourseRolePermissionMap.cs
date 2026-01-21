namespace Neura.Core.Abstractions.Consts;

public static class CourseRolePermissionMap
{
    public static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        [DefaultRoles.CourseOwner] = new[]
      {
            Permissions.GetCourses,Permissions.DeleteCourses , Permissions.UpdateCourses
        },
        [DefaultRoles.CoInstructor] = new[]
      {
             Permissions.GetCourses,Permissions.UpdateCourses
        },
        [DefaultRoles.TeachingAssistant] = new[]
      {
            Permissions.GetCourses
        },
        [DefaultRoles.Student] = new[]
      {
            Permissions.GetCourses
        }
    };

    public static readonly Dictionary<string, int> RolePermissionsMask = new()
    {
        [DefaultRoles.CourseOwner] =
            (int)CoursePermission.DeleteCourse | (int)CoursePermission.ViewCourse | (int)CoursePermission.UpdateCourse,
        [DefaultRoles.CoInstructor] =
            (int)CoursePermission.ViewCourse | (int)CoursePermission.UpdateCourse,
        [DefaultRoles.TeachingAssistant] =
            (int)CoursePermission.ViewCourse,
        [DefaultRoles.Student] =
            (int)CoursePermission.ViewCourse
    };

    public static readonly string[] AllPermissions =
    {
        Permissions.GetCourses,Permissions.DeleteCourses , Permissions.UpdateCourses
    };
}

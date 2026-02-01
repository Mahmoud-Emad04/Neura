namespace Neura.Core.Abstractions.Consts;

public static class CourseRolePermissionMap
{
    //public static readonly Dictionary<string, string[]> RolePermissions = new()
    //{
    //    [DefaultRoles.CourseOwner] = new[]
    //  {
    //        Permissions.GetCourses,Permissions.DeleteCourses , Permissions.UpdateCourses
    //    },
    //    [DefaultRoles.CoInstructor] = new[]
    //  {
    //         Permissions.GetCourses,Permissions.UpdateCourses
    //    },
    //    [DefaultRoles.TeachingAssistant] = new[]
    //  {
    //        Permissions.GetCourses
    //    },
    //    [DefaultRoles.Student] = new[]
    //  {
    //        Permissions.GetCourses
    //    }
    //};

    public static readonly Dictionary<string, int> RolePermissionsMask = new()
    {
        [DefaultRoles.CourseOwner] =
            (int)CoursePermission.AccessContent |
            (int)CoursePermission.GradeAssignment |
            (int)CoursePermission.UpdateCourse |
            (int)CoursePermission.DeleteCourse,

        [DefaultRoles.CoInstructor] =
            (int)CoursePermission.AccessContent |
            (int)CoursePermission.GradeAssignment |
            (int)CoursePermission.UpdateCourse,

        [DefaultRoles.TeachingAssistant] =
            (int)CoursePermission.AccessContent |
            (int)CoursePermission.GradeAssignment,

        [DefaultRoles.Student] =
            (int)CoursePermission.AccessContent |
            (int)CoursePermission.SubmitAssignment
    };

    public static readonly string[] AllPermissions =
    {
        Permissions.GetCourses, Permissions.DeleteCourses, Permissions.UpdateCourses
    };
}
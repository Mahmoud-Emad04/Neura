namespace Neura.Core.Abstractions.Consts;

public static class CoursePermissionMap
{
    public static readonly Dictionary<string, int> Map =
        new()
        {
            [Permissions.GetCourses] = (int)CoursePermission.ViewPublicDetails,
            [Permissions.UpdateCourses] = (int)CoursePermission.UpdateCourse,
            [Permissions.DeleteCourses] = (int)CoursePermission.DeleteCourse
        };
}
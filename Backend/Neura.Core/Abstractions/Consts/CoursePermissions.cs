namespace Neura.Core.Abstractions.Consts;

//TODO Change To ENUM

public static class CoursePermissions
{
    public static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        [DefaultRoles.CourseOwner] = new[]
      {
            "courses:update",
            "courses:delete",
            "courses:read",
            "lessons:add",
            "lessons:update",
            "students:read"
        },
        [DefaultRoles.CoInstructor] = new[]
      {
            "lessons:add",
            "lessons:update",
            "students:read"
        },
        [DefaultRoles.TeachingAssistant] = new[]
      {
            "students:view",
            "moderate:qna"
        },
        [DefaultRoles.Student] = new[]
      {
            "courses:read"
        }
    };

    public static readonly string[] AllPermissions =
    {
        "Course.ManageContent",
        "Course.ManageUsers",
        "Course.View",
        "Course.EditTopics",
        "Course.EditLessons",
    };
}

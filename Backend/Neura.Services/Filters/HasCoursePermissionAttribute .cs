using Microsoft.AspNetCore.Authorization;

namespace Neura.Services.Filters;

public class HasCoursePermissionAttribute : AuthorizeAttribute
{
    public const string PermissionPrefix = "Course";

    public HasCoursePermissionAttribute(string permission)
        : base($"{PermissionPrefix}:{permission}")
    {
    }
}
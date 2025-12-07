using Microsoft.AspNetCore.Authorization;

namespace Neura.Services.Filters;

public class HasCoursePermissionAttribute : AuthorizeAttribute
{
    public HasCoursePermissionAttribute(string permission)
        : base($"{PermissionPrefix}:{permission}") { }

    public const string PermissionPrefix = "Course";
}
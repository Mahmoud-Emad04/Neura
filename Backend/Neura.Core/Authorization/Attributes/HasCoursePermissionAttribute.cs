using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Core.Authorization.Attributes;

/// <summary>
///     Requires user to have the specified permission in the course
///     Course ID must be in route as {courseId} or {id}
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasCoursePermissionAttribute : AuthorizeAttribute
{
    public HasCoursePermissionAttribute(CoursePermission permission)
        : base($"CoursePermission_{permission}")
    {
    }
}
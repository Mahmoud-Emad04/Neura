using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Core.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasLessonPermissionAttribute : AuthorizeAttribute
{
    public HasLessonPermissionAttribute(CoursePermission permission)
        : base($"LessonPermission_{permission}")
    {
    }
}
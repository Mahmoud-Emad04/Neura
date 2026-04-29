using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Core.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasSectionPermissionAttribute : AuthorizeAttribute
{
    public HasSectionPermissionAttribute(CoursePermission permission)
        : base($"SectionPermission_{permission}")
    {
    }
}
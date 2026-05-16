using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Core.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasExamPermissionAttribute : AuthorizeAttribute
{
    public HasExamPermissionAttribute(CoursePermission permission)
        : base($"ExamPermission_{permission}")
    {
    }
}
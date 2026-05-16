using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Core.Authorization.Requirements;

public class ExamPermissionRequirement : IAuthorizationRequirement
{
    public ExamPermissionRequirement(CoursePermission permission)
    {
        Permission = permission;
    }

    public CoursePermission Permission { get; }
}
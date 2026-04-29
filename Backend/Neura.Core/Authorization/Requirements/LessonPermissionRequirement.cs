using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Core.Authorization.Requirements;

public class LessonPermissionRequirement : IAuthorizationRequirement
{
    public LessonPermissionRequirement(CoursePermission permission)
    {
        Permission = permission;
    }

    public CoursePermission Permission { get; }
}
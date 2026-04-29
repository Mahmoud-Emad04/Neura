using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Core.Authorization.Requirements;

public class SectionPermissionRequirement : IAuthorizationRequirement
{
    public SectionPermissionRequirement(CoursePermission permission)
    {
        Permission = permission;
    }

    public CoursePermission Permission { get; }
}
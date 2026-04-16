using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Core.Authorization.Requirements;

/// <summary>
///     Requirement for course-level permission authorization
/// </summary>
public class CoursePermissionRequirement : IAuthorizationRequirement
{
    public CoursePermissionRequirement(CoursePermission permission)
    {
        Permission = permission;
    }

    public CoursePermission Permission { get; }
}
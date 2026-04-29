using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Neura.Core.Authorization.Requirements;
using Neura.Core.Enums;
using Neura.Repository.Persistence.Authorization.Requirements;

namespace Neura.Services.Authorization.Providers;

/// <summary>
///     Dynamic policy provider for permission-based authorization
///     Creates policies on-demand based on policy name pattern
/// </summary>
/// ///
/// <summary>
///     Dynamic policy provider for permission-based authorization
///     Creates policies on-demand based on policy name pattern
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string GlobalRolePrefix = "GlobalRole_";
    private const string CoursePermissionPrefix = "CoursePermission_";
    private const string LessonPermissionPrefix = "LessonPermission_";
    private const string SectionPermissionPrefix = "SectionPermission_";
    private const string ExamPermissionPrefix = "ExamPermission_";

    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Handle GlobalRole policies (e.g., "GlobalRole_SuperAdmin")
        if (policyName.StartsWith(GlobalRolePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var roleName = policyName[GlobalRolePrefix.Length..];

            if (Enum.TryParse<GlobalRole>(roleName, true, out var role))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new GlobalRoleRequirement(role))
                    .Build();

                return policy;
            }
        }

        // Handle CoursePermission policies (e.g., "CoursePermission_EditContent")
        if (policyName.StartsWith(CoursePermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionName = policyName[CoursePermissionPrefix.Length..];

            if (Enum.TryParse<CoursePermission>(permissionName, true, out var permission))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new CoursePermissionRequirement(permission))
                    .Build();

                return policy;
            }
        }

        if (policyName.StartsWith(LessonPermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionName = policyName[LessonPermissionPrefix.Length..];

            if (Enum.TryParse<CoursePermission>(permissionName, true, out var permission))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new LessonPermissionRequirement(permission))
                    .Build();

                return policy;
            }
        }
        if (policyName.StartsWith(SectionPermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionName = policyName[SectionPermissionPrefix.Length..];

            if (Enum.TryParse<CoursePermission>(permissionName, true, out var permission))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new SectionPermissionRequirement(permission))
                    .Build();

                return policy;
            }
        }
        if (policyName.StartsWith(ExamPermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionName = policyName[ExamPermissionPrefix.Length..];

            if (Enum.TryParse<CoursePermission>(permissionName, true, out var permission))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ExamPermissionRequirement(permission))
                    .Build();

                return policy;
            }
        }

        // Fall back to default provider for other policies
        return await _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }
}
using Microsoft.AspNetCore.Authorization;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Enums;
using Neura.Repository.Persistence.Authorization.Requirements;

namespace Neura.Services.Authorization.Handlers;

/// <summary>
///     Handles global role-based authorization
/// </summary>
public class GlobalRoleAuthorizationHandler : AuthorizationHandler<GlobalRoleRequirement>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GlobalRoleAuthorizationHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GlobalRoleRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var userId = _userManager.GetUserId(context.User);

        if (string.IsNullOrEmpty(userId)) return;

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null) return;

        var userRoles = await _userManager.GetRolesAsync(user);
        var userHighestRole = GetHighestRole(userRoles);

        if (userHighestRole >= requirement.MinimumRole) context.Succeed(requirement);
    }

    private static GlobalRole GetHighestRole(IList<string> roles)
    {
        if (roles.Contains(DefaultRoles.SuperAdmin))
            return GlobalRole.SuperAdmin;

        if (roles.Contains(DefaultRoles.Admin))
            return GlobalRole.Admin;

        if (roles.Contains(DefaultRoles.Instructor))
            return GlobalRole.Instructor;

        if (roles.Contains(DefaultRoles.Member))
            return GlobalRole.Member;

        return GlobalRole.Guest;
    }
}
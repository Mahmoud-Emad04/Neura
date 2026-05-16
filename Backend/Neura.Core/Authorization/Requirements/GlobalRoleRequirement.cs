using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Repository.Persistence.Authorization.Requirements;

/// <summary>
///     Requirement for global role-based authorization
/// </summary>
public class GlobalRoleRequirement : IAuthorizationRequirement
{
    public GlobalRoleRequirement(GlobalRole minimumRole)
    {
        MinimumRole = minimumRole;
    }

    public GlobalRole MinimumRole { get; }
}
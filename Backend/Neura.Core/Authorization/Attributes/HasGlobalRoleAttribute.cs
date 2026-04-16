using Microsoft.AspNetCore.Authorization;
using Neura.Core.Enums;

namespace Neura.Core.Authorization.Attributes;

/// <summary>
///     Requires user to have at least the specified global role
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasGlobalRoleAttribute : AuthorizeAttribute
{
    public HasGlobalRoleAttribute(GlobalRole minimumRole)
        : base($"GlobalRole_{minimumRole}")
    {
    }
}
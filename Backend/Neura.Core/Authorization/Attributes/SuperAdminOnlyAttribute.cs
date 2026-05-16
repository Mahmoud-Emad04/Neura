using Microsoft.AspNetCore.Authorization;

namespace Neura.Core.Authorization.Attributes;

/// <summary>
///     Shortcut attribute for requiring SuperAdmin role
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SuperAdminOnlyAttribute : AuthorizeAttribute
{
    public SuperAdminOnlyAttribute()
        : base("GlobalRole_SuperAdmin")
    {
    }
}
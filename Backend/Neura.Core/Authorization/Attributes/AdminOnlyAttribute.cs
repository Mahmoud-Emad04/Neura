using Microsoft.AspNetCore.Authorization;

namespace Neura.Core.Authorization.Attributes;

/// <summary>
///     Shortcut attribute for requiring Admin role or higher
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute()
        : base("GlobalRole_Admin")
    {
    }
}
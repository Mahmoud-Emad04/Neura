using Microsoft.AspNetCore.Authorization;

namespace Neura.Core.Authorization.Attributes;

/// <summary>
///     Shortcut attribute for requiring Instructor role or higher
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class InstructorOnlyAttribute : AuthorizeAttribute
{
    public InstructorOnlyAttribute()
        : base("GlobalRole_Instructor")
    {
    }
}
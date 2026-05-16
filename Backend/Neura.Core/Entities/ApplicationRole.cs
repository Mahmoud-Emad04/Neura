using Microsoft.AspNetCore.Identity;

namespace Neura.Core.Entities;

public class ApplicationRole : IdentityRole
{
    /// <summary>
    ///     Is this a default system role?
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    ///     Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    ///     Description of what this role can do
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Hierarchy level for permission inheritance
    /// </summary>
    public int Level { get; set; }
}